using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultRefreshEndpointHandler : IRefreshEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authContext;
        private readonly IRefreshFlowService _refreshFlow;
        private readonly ICredentialResponseWriter _credentialWriter;
        private readonly IRefreshResponseWriter _refreshWriter;
        private readonly IRefreshTokenResolver _refreshTokenResolver;
        private readonly IRefreshResponsePolicy _refreshPolicy;

        public DefaultRefreshEndpointHandler(
            IAuthFlowContextAccessor authContext,
            IRefreshFlowService refreshFlow,
            ICredentialResponseWriter credentialWriter,
            IRefreshResponseWriter refreshWriter,
            IRefreshTokenResolver refreshTokenResolver,
            IRefreshResponsePolicy refreshPolicy)
        {
            _authContext = authContext;
            _refreshFlow = refreshFlow;
            _credentialWriter = credentialWriter;
            _refreshWriter = refreshWriter;
            _refreshTokenResolver = refreshTokenResolver;
            _refreshPolicy = refreshPolicy;
        }

        public async Task<IResult> RefreshAsync(HttpContext ctx)
        {
            var flow = _authContext.Current;

            if (flow.Session is not SessionSecurityContext session)
            {
                //_logger.LogDebug("Refresh called without active session.");
                return Results.Ok(RefreshOutcome.None);
            }

            var request = new RefreshFlowRequest
            {
                SessionId = session.SessionId,
                RefreshToken = _refreshTokenResolver.Resolve(ctx),
                Device = flow.Device,
                Now = DateTimeOffset.UtcNow
            };

            var result = await _refreshFlow.RefreshAsync(flow, request, ctx.RequestAborted);

            if (!result.Succeeded)
            {
                WriteRefreshHeader(ctx, flow, RefreshOutcome.ReauthRequired);
                return Results.Unauthorized();
            }

            var primary = _refreshPolicy.SelectPrimary(flow, request, result);

            if (primary == CredentialKind.Session && result.SessionId is not null)
            {
                _credentialWriter.Write(ctx, CredentialKind.Session, result.SessionId.Value);
            }
            else if (primary == CredentialKind.AccessToken && result.AccessToken is not null)
            {
                _credentialWriter.Write(ctx, CredentialKind.AccessToken, result.AccessToken);
            }

            if (_refreshPolicy.WriteRefreshToken(flow) && result.RefreshToken is not null)
            {
                _credentialWriter.Write(ctx, CredentialKind.RefreshToken, result.RefreshToken);
            }

            WriteRefreshHeader(ctx, flow, result.Outcome);
            return Results.NoContent();
        }

        private void WriteRefreshHeader(HttpContext ctx, AuthFlowContext flow, RefreshOutcome outcome)
        {
            if (!flow.OriginalOptions.Diagnostics.EnableRefreshHeaders)
                return;

            _refreshWriter.Write(ctx, outcome);
        }

    }
}
