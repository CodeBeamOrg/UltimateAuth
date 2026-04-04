using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users;
using Microsoft.Extensions.Options;

// TODO: Identifier-based throttling, Exponential lockout

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class LoginOrchestrator : ILoginOrchestrator, IInternalLoginOrchestrator
{
    private readonly ILoginIdentifierResolver _identifierResolver;
    private readonly IEnumerable<ICredentialProvider> _credentialProviders; // authentication
    private readonly IUserRuntimeStateProvider _users; // eligible
    private readonly ILoginAuthority _authority;
    private readonly ISessionOrchestrator _sessionOrchestrator;
    private readonly ITokenIssuer _tokens;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly ISessionStoreFactory _storeFactory;
    private readonly IAuthenticationSecurityManager _authenticationSecurityManager; // runtime risk
    private readonly UAuthEventDispatcher _events;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public LoginOrchestrator(
        ILoginIdentifierResolver identifierResolver,
        IEnumerable<ICredentialProvider> credentialProviders,
        IUserRuntimeStateProvider users,
        ILoginAuthority authority,
        ISessionOrchestrator sessionOrchestrator,
        ITokenIssuer tokens,
        IUserClaimsProvider claimsProvider,
        ISessionStoreFactory storeFactory,
        IAuthenticationSecurityManager authenticationSecurityManager,
        UAuthEventDispatcher events,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _identifierResolver = identifierResolver;
        _credentialProviders = credentialProviders;
        _users = users;
        _authority = authority;
        _sessionOrchestrator = sessionOrchestrator;
        _tokens = tokens;
        _claimsProvider = claimsProvider;
        _storeFactory = storeFactory;
        _authenticationSecurityManager = authenticationSecurityManager;
        _events = events;
        _options = options.Value;
        _clock = clock;
    }

    public Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
        => LoginAsync(flow, request, new LoginExecutionOptions { Mode = LoginExecutionMode.Commit }, ct);

    public async Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, LoginExecutionOptions loginExecution, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (flow.Device.DeviceId is not DeviceId deviceId)
            throw new UAuthConflictException("Device id could not resolved.");

        var now = _clock.UtcNow;
        var resolution = await _identifierResolver.ResolveAsync(flow.Tenant, request.Identifier, ct);
        var userKey = resolution?.UserKey;

        bool userExists = false;
        bool credentialsValid = false;

        AuthenticationSecurityState? accountState = null;
        AuthenticationSecurityState? factorState = null;

        if (userKey is not null)
        {
            var user = await _users.GetAsync(flow.Tenant, userKey.Value, ct);
            if (user is not null && user.CanAuthenticate && !user.IsDeleted)
            {
                userExists = true;
                accountState = await _authenticationSecurityManager.GetOrCreateAccountAsync(flow.Tenant, userKey.Value, ct);

                if (accountState.IsLocked(now))
                {
                    return LoginResult.Failed(AuthFailureReason.LockedOut, accountState.LockedUntil, remainingAttempts: 0);
                }

                factorState = await _authenticationSecurityManager.GetOrCreateFactorAsync(flow.Tenant, userKey.Value, request.Factor, ct);

                if (factorState.IsLocked(now))
                {
                    return LoginResult.Failed(AuthFailureReason.LockedOut, factorState.LockedUntil, 0);
                }

                foreach (var provider in _credentialProviders)
                {
                    var credentials = await provider.GetByUserAsync(flow.Tenant, userKey.Value, ct);

                    foreach (var credential in credentials)
                    {
                        if (credential.IsDeleted || !credential.Security.IsUsable(now))
                            continue;

                        if (await provider.ValidateAsync(credential, request.Secret, ct))
                        {
                            credentialsValid = true;
                            break;
                        }
                    }

                    if (credentialsValid)
                        break;
                }
            }
        }

        // TODO: Add create-time uniqueness guard for chain id for concurrency
        var sessionStore = _storeFactory.Create(flow.Tenant);
        SessionChainId? chainId = null;

        if (userKey is not null)
        {
            var chain = await sessionStore.GetChainByDeviceAsync(userKey.Value, deviceId, ct);

            if (chain is not null && !chain.IsRevoked)
                chainId = chain.ChainId;
        }

        // TODO: Add accountState here, currently it only checks factor state
        var decisionContext = new LoginDecisionContext
        {
            Tenant = flow.Tenant,
            Identifier = request.Identifier,
            CredentialsValid = credentialsValid,
            UserExists = userExists,
            UserKey = userKey,
            SecurityState = factorState,
            IsChained = chainId is not null
        };

        var decision = _authority.Decide(decisionContext);

        if (decision.Kind == LoginDecisionKind.Deny)
        {
            if (userKey is not null && userExists && factorState is not null)
            {
                DateTimeOffset? lockedUntil = null;
                int? remainingAttempts = null;

                if (!loginExecution.SuppressFailureAttempt)
                {
                    var version = factorState.SecurityVersion;
                    factorState = factorState.RegisterFailure(now, _options.Login.MaxFailedAttempts, _options.Login.LockoutDuration, _options.Login.ExtendLockOnFailure);
                    await _authenticationSecurityManager.UpdateAsync(factorState, version, ct);
                }

                if (_options.Login.IncludeFailureDetails)
                {
                    var stateForResponse = factorState;

                    if (stateForResponse.IsLocked(now))
                    {
                        lockedUntil = stateForResponse.LockedUntil;
                        remainingAttempts = 0;
                    }
                    else if (_options.Login.MaxFailedAttempts > 0)
                    {
                        remainingAttempts = _options.Login.MaxFailedAttempts - stateForResponse.FailedAttempts;
                    }
                }


                return LoginResult.Failed(
                    factorState.IsLocked(now)
                        ? AuthFailureReason.LockedOut
                        : decision.FailureReason,
                    lockedUntil,
                    remainingAttempts);
            }

            return LoginResult.Failed(decision.FailureReason);
        }


        if (decision.Kind == LoginDecisionKind.Challenge)
        {
            return LoginResult.Continue(new LoginContinuation
            {
                Type = LoginContinuationType.Mfa
            });
        }

        if (!credentialsValid || userKey is null)
            return LoginResult.Failed(AuthFailureReason.InvalidCredentials);

        // After this point, the login is successful. We can reset any failure counts and proceed to create a session.
        if (loginExecution.Mode == LoginExecutionMode.Preview)
        {
            return LoginResult.SuccessPreview();
        }

        if (!loginExecution.SuppressSuccessReset && factorState is not null)
        {
            var version = factorState.SecurityVersion;
            factorState = factorState.RegisterSuccess();
            await _authenticationSecurityManager.UpdateAsync(factorState, version, ct);
        }

        var claims = await _claimsProvider.GetClaimsAsync(flow.Tenant, userKey.Value, ct);

        var sessionContext = new SessionIssuanceContext
        {
            Tenant = flow.Tenant,
            UserKey = userKey.Value,
            Now = now,
            Device = flow.Device,
            Claims = claims,
            ChainId = chainId,
            Metadata = SessionMetadata.Empty,
            Mode = flow.EffectiveMode
        };

        var authContext = flow.ToAuthContext(now);
        var issuedSession = await _sessionOrchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand(sessionContext), ct);

        AuthTokens? tokens = null;

        if (request.RequestTokens)
        {
            var tokenContext = new TokenIssuanceContext
            {
                Tenant = flow.Tenant,
                UserKey = userKey.Value,
                SessionId = issuedSession.Session.SessionId,
                ChainId = issuedSession.Session.ChainId,
                Claims = claims.AsDictionary()
            };

            tokens = new AuthTokens
            {
                AccessToken = await _tokens.IssueAccessTokenAsync(flow, tokenContext, ct),
                RefreshToken = await _tokens.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.Persist, ct)
            };
        }

        await _events.DispatchAsync(
            new UserLoggedInContext(flow.Tenant, userKey.Value, now, flow.Device, issuedSession.Session.SessionId));

        return LoginResult.Success(issuedSession.Session.SessionId, tokens);
    }
}
