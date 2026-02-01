using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Abstractions;

namespace CodeBeam.UltimateAuth.Server.Login.Orchestrators
{
    internal sealed class DefaultLoginOrchestrator<TUserId> : ILoginOrchestrator<TUserId>
    {
        private readonly ICredentialStore<TUserId> _credentialStore; // authentication
        private readonly ICredentialValidator _credentialValidator;
        private readonly IUserRuntimeStateProvider _users; // eligible
        private readonly IUserSecurityStateProvider<TUserId> _userSecurityStateProvider; // runtime risk
        private readonly ILoginAuthority _authority;
        private readonly ISessionOrchestrator _sessionOrchestrator;
        private readonly ITokenIssuer _tokens;
        private readonly IUserClaimsProvider _claimsProvider;
        private readonly IUserIdConverterResolver _userIdConverterResolver;

        public DefaultLoginOrchestrator(
            ICredentialStore<TUserId> credentialStore,
            ICredentialValidator credentialValidator,
            IUserRuntimeStateProvider users,
            IUserSecurityStateProvider<TUserId> userSecurityStateProvider,
            ILoginAuthority authority,
            ISessionOrchestrator sessionOrchestrator,
            ITokenIssuer tokens,
            IUserClaimsProvider claimsProvider,
            IUserIdConverterResolver userIdConverterResolver)
        {
            _credentialStore = credentialStore;
            _credentialValidator = credentialValidator;
            _users = users;
            _userSecurityStateProvider = userSecurityStateProvider;
            _authority = authority;
            _sessionOrchestrator = sessionOrchestrator;
            _tokens = tokens;
            _claimsProvider = claimsProvider;
            _userIdConverterResolver = userIdConverterResolver;
        }

        public async Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var now = request.At ?? DateTimeOffset.UtcNow;

            var credentials = await _credentialStore.FindByLoginAsync(request.TenantId, request.Identifier, ct);
            var orderedCredentials = credentials
                .OfType<ISecurableCredential>()
                .Where(c => c.Security.IsUsable(now))
                .Cast<ICredential<TUserId>>()
                .ToList();

            TUserId validatedUserId = default!;
            bool credentialsValid = false;

            foreach (var credential in orderedCredentials)
            {
                var result = await _credentialValidator.ValidateAsync(credential, request.Secret, ct);

                if (result.IsValid)
                {
                    validatedUserId = credential.UserId;
                    credentialsValid = true;
                    break;
                }
            }

            bool userExists = credentialsValid;

            IUserSecurityState? securityState = null;
            UserKey? userKey = null;

            if (credentialsValid)
            {
                securityState = await _userSecurityStateProvider.GetAsync(request.TenantId, validatedUserId, ct);
                var converter = _userIdConverterResolver.GetConverter<TUserId>();
                userKey = UserKey.FromString(converter.ToCanonicalString(validatedUserId));
            }

            var user = userKey is not null
                ? await _users.GetAsync(request.TenantId, userKey.Value, ct)
                : null;

            if (user is null || user.IsDeleted || !user.IsActive)
            {
                // Deliberately vague
                return LoginResult.Failed();
            }

            var decisionContext = new LoginDecisionContext
            {
                TenantId = request.TenantId,
                Identifier = request.Identifier,
                CredentialsValid = credentialsValid,
                UserExists = userExists,
                UserKey = userKey,
                SecurityState = securityState,
                IsChained = request.ChainId is not null
            };

            var decision = _authority.Decide(decisionContext);

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

            if (userKey is not UserKey validUserKey)
            {
                return LoginResult.Failed();
            }

            var claims = await _claimsProvider.GetClaimsAsync(request.TenantId, validUserKey, ct);

            var sessionContext = new AuthenticatedSessionContext
            {
                TenantId = request.TenantId,
                UserKey = validUserKey,
                Now = now,
                Device = request.Device,
                Claims = claims,
                ChainId = request.ChainId,
                Metadata = SessionMetadata.Empty
            };

            var authContext = flow.ToAuthContext(now);
            var issuedSession = await _sessionOrchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand<TUserId>(sessionContext), ct);

            AuthTokens? tokens = null;

            if (request.RequestTokens)
            {
                var tokenContext = new TokenIssuanceContext
                {
                    TenantId = request.TenantId,
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

            return LoginResult.Success(issuedSession.Session.SessionId, tokens);

        }
    }
}
