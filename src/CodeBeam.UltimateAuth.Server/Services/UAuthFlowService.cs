using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Login;
using System;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthFlowService<TUserId> : IUAuthFlowService<TUserId>
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly ILoginOrchestrator<TUserId> _loginOrchestrator;
        private readonly ISessionOrchestrator _orchestrator;
        private readonly ISessionQueryService _queries;
        private readonly ITokenIssuer _tokens;
        private readonly IUserIdConverterResolver _userIdConverterResolver;
        private readonly IRefreshTokenValidator _tokenValidator;

        public UAuthFlowService(
            IAuthFlowContextAccessor authFlow,
            ILoginOrchestrator<TUserId> loginOrchestrator,
            ISessionOrchestrator orchestrator,
            ISessionQueryService queries,
            ITokenIssuer tokens,
            IUserIdConverterResolver userIdConverterResolver,
            IRefreshTokenValidator tokenValidator)
        {
            _authFlow = authFlow;
            _loginOrchestrator = loginOrchestrator;
            _orchestrator = orchestrator;
            _queries = queries;
            _tokens = tokens;
            _userIdConverterResolver = userIdConverterResolver;
            _tokenValidator = tokenValidator;
        }

        public Task<MfaChallengeResult> BeginMfaAsync(BeginMfaRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResult> CompleteMfaAsync(CompleteMfaRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
        {
            return _loginOrchestrator.LoginAsync(flow, request, ct);
        }

        public Task<LoginResult> LoginAsync(AuthFlowContext flow, AuthExecutionContext execution, LoginRequest request, CancellationToken ct = default)
        {
            var effectiveFlow = execution.EffectiveClientProfile is null
                ? flow
                : flow.WithClientProfile((UAuthClientProfile)execution.EffectiveClientProfile);
            return _loginOrchestrator.LoginAsync(effectiveFlow, request, ct);
        }

        public Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        {
            var authFlow = _authFlow.Current;
            var now = request.At ?? DateTimeOffset.UtcNow;
            var authContext = authFlow.ToAuthContext(now);

            return _orchestrator.ExecuteAsync(authContext, new RevokeSessionCommand(request.SessionId), ct);
        }

        public async Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default)
        {
            var authFlow = _authFlow.Current;
            var now = request.At ?? DateTimeOffset.UtcNow;

            if (authFlow.Session is not SessionSecurityContext session)
                throw new InvalidOperationException("LogoutAll requires an active session.");

            var authContext = authFlow.ToAuthContext(now);
            SessionChainId? exceptChainId = null;

            if (request.ExceptCurrent)
            {
                exceptChainId = session.ChainId;

                if (exceptChainId is null)
                    throw new InvalidOperationException("Current session chain could not be resolved.");
            }

            if (authFlow.UserKey is UserKey uaKey)
            {
                var command = new RevokeAllChainsCommand(uaKey, exceptChainId);
                await _orchestrator.ExecuteAsync(authContext, command, ct);
            }
            
        }

        public Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

    }
}
