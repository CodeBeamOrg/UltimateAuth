using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users;

using CodeBeam.UltimateAuth.Core;

namespace CodeBeam.UltimateAuth.Server.Login.Orchestrators
{
    internal sealed class DefaultLoginOrchestrator<TUserId> : ILoginOrchestrator<TUserId>
    {
        private readonly ICredentialStore<TUserId> _credentialStore;
        private readonly ICredentialValidator _credentialValidator;
        private readonly IUserStore<TUserId> _users;
        private readonly IUserSecurityStateProvider<TUserId> _userSecurityStateProvider;
        private readonly ILoginAuthority _authority;
        private readonly ISessionOrchestrator _sessionOrchestrator;
        private readonly ITokenIssuer _tokens;
        private readonly IUserClaimsProvider _claimsProvider;
        private readonly IUserIdConverterResolver _userIdConverterResolver;

        public DefaultLoginOrchestrator(
            ICredentialStore<TUserId> credentialStore,
            ICredentialValidator credentialValidator,
            IUserStore<TUserId> users,
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
            var now = request.At ?? DateTimeOffset.UtcNow;

            // 1. Validate credentials (Credentials domain)
            var credential = await _credentialStore.FindByLoginAsync(request.TenantId, request.Identifier, ct);
            bool credentialsValid = false;
            TUserId? userId = default;

            if (credential is not null)
            {
                var credentialValidationResult = await _credentialValidator.ValidateAsync(credential, request.Secret, ct);
                credentialsValid = credentialValidationResult.IsValid;
                userId = credential.UserId;
            }

            bool userExists = userId is not null;

            // 2. Resolve user security state (Users domain)
            IUserSecurityState? securityState = null;
            UserKey? userKey = null;

            if (userExists)
            {
                securityState = await _userSecurityStateProvider.GetAsync(request.TenantId, userId!, ct);
                var converter = _userIdConverterResolver.GetConverter<TUserId>();
                userKey = UserKey.FromString(converter.ToString(userId!));
            }

            // 3. Authority decision (Login domain)
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

            switch (decision.Kind)
            {
                case LoginDecisionKind.Deny:
                    return LoginResult.Failed();

                case LoginDecisionKind.Challenge:
                    {
                        // Orchestrator decides HOW to continue
                        var continuation = new LoginContinuation
                        {
                            Type = LoginContinuationType.Mfa,
                            // TODO: Add here
                            //ContinuationToken = _continuationTokenService.Create(
                            //    request.TenantId,
                            //    userKey!,
                            //    request.ChainId),
                            Hint = decision.Reason
                        };

                        return LoginResult.Continue(continuation);
                    }

                case LoginDecisionKind.Allow:
                    break;
            }

            if (userKey is not UserKey validUserKey)
            {
                return LoginResult.Failed();
            }

            var claims = await _claimsProvider.GetClaimsAsync(request.TenantId, validUserKey, ct);

            // 4. Create authenticated session
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

            // 6. Issue tokens if requested
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

                var access = await _tokens.IssueAccessTokenAsync(flow, tokenContext, ct);
                var refresh = await _tokens.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.Persist, ct);

                tokens = new AuthTokens
                {
                    AccessToken = access,
                    RefreshToken = refresh
                };
            }

            return LoginResult.Success(issuedSession.Session.SessionId, tokens);
        }
    }
}
