using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
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

internal sealed class LoginOrchestrator : ILoginOrchestrator
{
    private readonly ILoginIdentifierResolver _identifierResolver;
    private readonly ICredentialStore _credentialStore; // authentication
    private readonly ICredentialValidator _credentialValidator;
    private readonly IUserRuntimeStateProvider _users; // eligible
    private readonly ILoginAuthority _authority;
    private readonly ISessionOrchestrator _sessionOrchestrator;
    private readonly ITokenIssuer _tokens;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IAuthenticationSecurityManager _authenticationSecurityManager; // runtime risk
    private readonly UAuthEventDispatcher _events;
    private readonly UAuthServerOptions _options;

    public LoginOrchestrator(
        ILoginIdentifierResolver identifierResolver,
        ICredentialStore credentialStore,
        ICredentialValidator credentialValidator,
        IUserRuntimeStateProvider users,
        ILoginAuthority authority,
        ISessionOrchestrator sessionOrchestrator,
        ITokenIssuer tokens,
        IUserClaimsProvider claimsProvider,
        IAuthenticationSecurityManager authenticationSecurityManager,
        UAuthEventDispatcher events,
        IOptions<UAuthServerOptions> options)
    {
        _identifierResolver = identifierResolver;
        _credentialStore = credentialStore;
        _credentialValidator = credentialValidator;
        _users = users;
        _authority = authority;
        _sessionOrchestrator = sessionOrchestrator;
        _tokens = tokens;
        _claimsProvider = claimsProvider;
        _authenticationSecurityManager = authenticationSecurityManager;
        _events = events;
        _options = options.Value;
    }

    public async Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = request.At ?? DateTimeOffset.UtcNow;
        var resolution = await _identifierResolver.ResolveAsync(request.Tenant, request.Identifier, ct);
        var userKey = resolution?.UserKey;

        bool userExists = false;
        bool credentialsValid = false;

        AuthenticationSecurityState? accountState = null;
        AuthenticationSecurityState? factorState = null;

        if (userKey is not null)
        {
            var user = await _users.GetAsync(request.Tenant, userKey.Value, ct);
            if (user is not null && user.IsActive && !user.IsDeleted)
            {
                userExists = true;

                accountState = await _authenticationSecurityManager.GetOrCreateAccountAsync(request.Tenant, userKey.Value, ct);

                if (accountState.IsLocked(now))
                {
                    return LoginResult.Failed(AuthFailureReason.LockedOut, accountState.LockedUntil, remainingAttempts: 0);
                }

                factorState = await _authenticationSecurityManager.GetOrCreateFactorAsync(request.Tenant, userKey.Value, request.Factor, ct);

                if (factorState.IsLocked(now))
                {
                    return LoginResult.Failed(AuthFailureReason.LockedOut, factorState.LockedUntil, 0);
                }

                var credentials = await _credentialStore.GetByUserAsync(request.Tenant, userKey.Value, ct);

                // TODO: Add .Where(c => c.Type == request.Factor) when we support multiple factors per user
                foreach (var credential in credentials.OfType<ICredentialDescriptor>())
                {
                    if (!credential.Security.IsUsable(now))
                        continue;

                    var result = await _credentialValidator.ValidateAsync((ICredential)credential, request.Secret, ct);

                    if (result.IsValid)
                    {
                        credentialsValid = true;
                        break;
                    }
                }
            }
        }

        // TODO: Add accountState here, currently it only checks factor state
        var decisionContext = new LoginDecisionContext
        {
            Tenant = request.Tenant,
            Identifier = request.Identifier,
            CredentialsValid = credentialsValid,
            UserExists = userExists,
            UserKey = userKey,
            SecurityState = factorState,
            IsChained = request.ChainId is not null
        };

        var decision = _authority.Decide(decisionContext);

        var max = _options.Login.MaxFailedAttempts;

        if (decision.Kind == LoginDecisionKind.Deny)
        {
            if (userKey is not null && userExists && factorState is not null)
            {
                var securityVersion = factorState.SecurityVersion;
                factorState = factorState.RegisterFailure(now, _options.Login.MaxFailedAttempts, _options.Login.LockoutDuration, _options.Login.ExtendLockOnFailure);
                await _authenticationSecurityManager.UpdateAsync(factorState, securityVersion, ct);

                DateTimeOffset? lockedUntil = null;
                int? remainingAttempts = null;

                if (_options.Login.IncludeFailureDetails)
                {
                    if (factorState.IsLocked(now))
                    {
                        lockedUntil = factorState.LockedUntil;
                        remainingAttempts = 0;
                    }
                    else if (_options.Login.MaxFailedAttempts > 0)
                    {
                        remainingAttempts = _options.Login.MaxFailedAttempts - factorState.FailedAttempts;
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
        if (factorState is not null)
        {
            var version = factorState.SecurityVersion;
            factorState = factorState.RegisterSuccess();
            await _authenticationSecurityManager.UpdateAsync(factorState, version, ct);
        }

        var claims = await _claimsProvider.GetClaimsAsync(request.Tenant, userKey.Value, ct);

        var sessionContext = new AuthenticatedSessionContext
        {
            Tenant = request.Tenant,
            UserKey = userKey.Value,
            Now = now,
            Device = flow.Device,
            Claims = claims,
            ChainId = request.ChainId,
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
                Tenant = request.Tenant,
                UserKey = userKey.Value,
                SessionId = issuedSession.Session.SessionId,
                ChainId = request.ChainId,
                Claims = claims.AsDictionary()
            };

            tokens = new AuthTokens
            {
                AccessToken = await _tokens.IssueAccessTokenAsync(flow, tokenContext, ct),
                RefreshToken = await _tokens.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.Persist, ct)
            };
        }

        await _events.DispatchAsync(
            new UserLoggedInContext(request.Tenant, userKey.Value, now, flow.Device, issuedSession.Session.SessionId));

        return LoginResult.Success(issuedSession.Session.SessionId, tokens);
    }
}
