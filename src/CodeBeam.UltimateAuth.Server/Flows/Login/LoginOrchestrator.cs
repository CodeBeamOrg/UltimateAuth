using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class LoginOrchestrator<TUserId> : ILoginOrchestrator<TUserId>
{
    private readonly ICredentialStore<TUserId> _credentialStore; // authentication
    private readonly ICredentialValidator _credentialValidator;
    private readonly IUserRuntimeStateProvider _users; // eligible
    private readonly ILoginAuthority _authority;
    private readonly ISessionOrchestrator _sessionOrchestrator;
    private readonly ITokenIssuer _tokens;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IUserIdConverterResolver _userIdConverterResolver;
    private readonly IUserSecurityStateWriter<TUserId> _securityWriter;
    private readonly IUserSecurityStateProvider<TUserId> _securityStateProvider; // runtime risk
    private readonly UAuthEventDispatcher _events;
    private readonly UAuthServerOptions _options;

    public LoginOrchestrator(
        ICredentialStore<TUserId> credentialStore,
        ICredentialValidator credentialValidator,
        IUserRuntimeStateProvider users,
        ILoginAuthority authority,
        ISessionOrchestrator sessionOrchestrator,
        ITokenIssuer tokens,
        IUserClaimsProvider claimsProvider,
        IUserIdConverterResolver userIdConverterResolver,
        IUserSecurityStateWriter<TUserId> securityWriter,
        IUserSecurityStateProvider<TUserId> securityStateProvider,
        UAuthEventDispatcher events,
        IOptions<UAuthServerOptions> options)
    {
        _credentialStore = credentialStore;
        _credentialValidator = credentialValidator;
        _users = users;
        _authority = authority;
        _sessionOrchestrator = sessionOrchestrator;
        _tokens = tokens;
        _claimsProvider = claimsProvider;
        _userIdConverterResolver = userIdConverterResolver;
        _securityWriter = securityWriter;
        _securityStateProvider = securityStateProvider;
        _events = events;
        _options = options.Value;
    }

    public async Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = request.At ?? DateTimeOffset.UtcNow;
        var credentials = await _credentialStore.FindByLoginAsync(request.Tenant, request.Identifier, ct);

        bool hasCandidateUser = false;
        TUserId candidateUserId = default!;
        TUserId validatedUserId = default!;
        bool credentialsValid = false;

        foreach (var credential in credentials.OfType<ISecurableCredential>())
        {
            if (!credential.Security.IsUsable(now))
                continue;

            var typed = (ICredential<TUserId>)credential;

            if (!hasCandidateUser)
            {
                candidateUserId = typed.UserId;
                hasCandidateUser = true;
            }

            var result = await _credentialValidator.ValidateAsync((ICredential<TUserId>)credential, request.Secret, ct);

            if (result.IsValid)
            {
                validatedUserId = ((ICredential<TUserId>)credential).UserId;
                credentialsValid = true;
                break;
            }
        }

        bool userExists = false;
        IUserSecurityState? securityState = null;
        UserKey? userKey = null;

        if (candidateUserId is not null)
        {
            securityState = await _securityStateProvider.GetAsync(request.Tenant, candidateUserId, ct);
            var converter = _userIdConverterResolver.GetConverter<TUserId>();
            var canonicalUserId = converter.ToCanonicalString(candidateUserId);

            if (!string.IsNullOrWhiteSpace(canonicalUserId))
            {
                var tempUserKey = UserKey.FromString(canonicalUserId);
                var user = await _users.GetAsync(request.Tenant, tempUserKey, ct);
                if (user is not null && user.IsActive && !user.IsDeleted)
                {
                    userKey = tempUserKey;
                    userExists = true;
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

        if (candidateUserId is not null)
        {
            if (decision.Kind == LoginDecisionKind.Allow)
            {
                await _securityWriter.ResetFailuresAsync(request.Tenant, candidateUserId, ct);
            }
            else
            {
                var isCurrentlyLocked = securityState?.IsLocked == true && securityState?.LockedUntil is DateTimeOffset until && until > now;

                if (!isCurrentlyLocked)
                {
                    await _securityWriter.RecordFailedLoginAsync(request.Tenant, candidateUserId, now, ct);

                    var currentFailures = securityState?.FailedLoginAttempts ?? 0;
                    var nextCount = currentFailures + 1;

                    if (_options.Login.MaxFailedAttempts > 0 && nextCount >= _options.Login.MaxFailedAttempts)
                    {
                        var lockedUntil = now.AddMinutes(_options.Login.LockoutMinutes);
                        await _securityWriter.LockUntilAsync(request.Tenant, candidateUserId, lockedUntil, ct);
                    }
                }
                
            }
        }

        if (decision.Kind == LoginDecisionKind.Deny)
            return LoginResult.Failed();

        if (decision.Kind == LoginDecisionKind.Challenge)
        {
            return LoginResult.Continue(new LoginContinuation
            {
                Type = LoginContinuationType.Mfa,
                Hint = decision.Reason
            });
        }

        if (validatedUserId is null || userKey is not UserKey validUserKey)
            return LoginResult.Failed();

        var claims = await _claimsProvider.GetClaimsAsync(request.Tenant, validUserKey, ct);

        var sessionContext = new AuthenticatedSessionContext
        {
            Tenant = request.Tenant,
            UserKey = validUserKey,
            Now = now,
            Device = request.Device,
            Claims = claims,
            ChainId = request.ChainId,
            Metadata = SessionMetadata.Empty,
            Mode = flow.EffectiveMode
        };

        var authContext = flow.ToAuthContext(now);
        var issuedSession = await _sessionOrchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand<TUserId>(sessionContext), ct);

        AuthTokens? tokens = null;

        if (request.RequestTokens)
        {
            var tokenContext = new TokenIssuanceContext
            {
                Tenant = request.Tenant,
                UserKey = validUserKey,
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

        await _events.DispatchAsync(new UserLoggedInContext(request.Tenant, validUserKey, now, request.Device, issuedSession.Session.SessionId));

        return LoginResult.Success(issuedSession.Session.SessionId, tokens);

    }
}
