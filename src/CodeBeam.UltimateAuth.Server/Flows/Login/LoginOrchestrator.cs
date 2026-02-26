using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Events;
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
    private readonly IUserSecurityStateWriter _securityWriter;
    private readonly IUserSecurityStateProvider _securityStateProvider; // runtime risk
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
        IUserSecurityStateWriter securityWriter,
        IUserSecurityStateProvider securityStateProvider,
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
        _securityWriter = securityWriter;
        _securityStateProvider = securityStateProvider;
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

        IUserSecurityState? securityState = null;

        DateTimeOffset? lockoutUntilUtc = null;
        int? remainingAttempts = null;

        if (userKey is not null)
        {
            var user = await _users.GetAsync(request.Tenant, userKey.Value, ct);
            if (user is not null && user.IsActive && !user.IsDeleted)
            {
                userExists = true;

                securityState = await _securityStateProvider.GetAsync(request.Tenant, userKey.Value, ct);

                if (securityState?.LastFailedAt is DateTimeOffset lastFail && _options.Login.FailureWindow is { } window && now - lastFail > window)
                {
                    await _securityWriter.ResetFailuresAsync(request.Tenant, userKey.Value, ct);
                    securityState = null;
                }

                if (securityState?.LockedUntil is DateTimeOffset until && until <= now)
                {
                    await _securityWriter.ResetFailuresAsync(request.Tenant, userKey.Value, ct);
                    securityState = null;
                }

                if (securityState?.LockedUntil is DateTimeOffset stillLocked && stillLocked > now)
                {
                    return LoginResult.Failed(AuthFailureReason.LockedOut, stillLocked, 0);
                }

                var credentials = await _credentialStore.GetByUserAsync(request.Tenant, userKey.Value, ct);

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

        var decisionContext = new LoginDecisionContext
        {
            Tenant = request.Tenant,
            Identifier = request.Identifier,
            CredentialsValid = credentialsValid,
            UserExists = userExists,
            UserKey = userKey,
            SecurityState = securityState,
            IsChained = request.ChainId is not null
        };

        var decision = _authority.Decide(decisionContext);

        var max = _options.Login.MaxFailedAttempts;

        if (decision.Kind == LoginDecisionKind.Deny)
        {
            if (userKey is not null && userExists)
            {
                var isCurrentlyLocked =
                    securityState?.IsLocked == true &&
                    securityState?.LockedUntil is DateTimeOffset until &&
                    until > now;

                if (!isCurrentlyLocked)
                {
                    await _securityWriter.RecordFailedLoginAsync(request.Tenant, userKey.Value, now, ct);

                    var currentFailures = securityState?.FailedLoginAttempts ?? 0;
                    var nextCount = currentFailures + 1;

                    if (max > 0)
                    {
                        if (nextCount >= max)
                        {
                            lockoutUntilUtc = now.Add(_options.Login.LockoutDuration);
                            await _securityWriter.LockUntilAsync(request.Tenant, userKey.Value, lockoutUntilUtc.Value, ct);
                            remainingAttempts = 0;

                            return LoginResult.Failed(AuthFailureReason.LockedOut, lockoutUntilUtc, remainingAttempts);
                        }
                        else
                        {
                            remainingAttempts = max - nextCount;
                        }
                    }
                }
                else
                {
                    lockoutUntilUtc = securityState!.LockedUntil;
                    remainingAttempts = 0;
                }
            }

            return LoginResult.Failed(decision.FailureReason, lockoutUntilUtc, remainingAttempts);
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
        await _securityWriter.ResetFailuresAsync(request.Tenant, userKey.Value, ct);

        var claims = await _claimsProvider.GetClaimsAsync(request.Tenant, userKey.Value, ct);

        var sessionContext = new AuthenticatedSessionContext
        {
            Tenant = request.Tenant,
            UserKey = userKey.Value,
            Now = now,
            Device = request.Device,
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
            new UserLoggedInContext(request.Tenant, userKey.Value, now, request.Device, issuedSession.Session.SessionId));

        return LoginResult.Success(issuedSession.Session.SessionId, tokens);
    }
}
