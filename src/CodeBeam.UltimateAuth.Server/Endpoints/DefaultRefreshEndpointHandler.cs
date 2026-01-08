using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultRefreshEndpointHandler<TUserId> : IRefreshEndpointHandler where TUserId : notnull
    {
        private readonly IAuthFlowContextAccessor _authContext;
        private readonly ISessionRefreshService<TUserId> _sessionRefresh;
        private readonly IRefreshTokenRotationService<TUserId> _tokenRotation;
        private readonly ICredentialResponseWriter _credentialWriter;
        private readonly IRefreshResponseWriter _refreshWriter;
        private readonly ISessionQueryService<TUserId> _sessionQueries;
        private readonly IRefreshTokenResolver _refreshTokenResolver;

        public DefaultRefreshEndpointHandler(
            IAuthFlowContextAccessor authContext,
            ISessionRefreshService<TUserId> sessionRefresh,
            IRefreshTokenRotationService<TUserId> tokenRotation,
            ICredentialResponseWriter credentialWriter,
            IRefreshResponseWriter refreshWriter,
            ISessionQueryService<TUserId> sessionQueries,
            IRefreshTokenResolver refreshTokenResolver)
        {
            _authContext = authContext;
            _sessionRefresh = sessionRefresh;
            _tokenRotation = tokenRotation;
            _credentialWriter = credentialWriter;
            _refreshWriter = refreshWriter;
            _sessionQueries = sessionQueries;
            _refreshTokenResolver = refreshTokenResolver;
        }

        public async Task<IResult> RefreshAsync(HttpContext ctx)
        {
            var auth = _authContext.Current;
            var decision = RefreshDecisionResolver.Resolve(auth.EffectiveMode);

            return decision switch
            {
                RefreshDecision.SessionTouch => await HandleSessionTouchAsync(ctx, auth, auth.Response),
                RefreshDecision.TokenRotation => await HandleTokenRotationAsync(ctx, auth, auth.Response),

                _ => Results.StatusCode(StatusCodes.Status409Conflict)
            };
        }

        private async Task<IResult> HandleSessionTouchAsync(HttpContext ctx, AuthFlowContext flow, EffectiveAuthResponse response)
        {
            if (flow.SessionId is null)
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            var validation = await _sessionQueries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = flow.TenantId,
                    SessionId = flow.SessionId.Value,
                    Now = now,
                    Device = DeviceInfoFactory.FromHttpContext(ctx)
                },
                ctx.RequestAborted);

            if (!validation.IsValid)
            {
                WriteRefreshHeader(ctx, flow, RefreshOutcome.ReauthRequired);
                return Results.Unauthorized();
            }

            var result = await _sessionRefresh.RefreshAsync(validation, now, ctx.RequestAborted);

            if (!result.IsSuccess || result.PrimaryToken is null)
            {
                WriteRefreshHeader(ctx, flow, RefreshOutcome.ReauthRequired);
                return Results.Unauthorized();
            }

            _credentialWriter.Write(ctx, CredentialKind.Session, result.PrimaryToken.Value);
            WriteRefreshHeader(ctx, flow, result.DidTouch ? RefreshOutcome.Touched : RefreshOutcome.NoOp);

            return Results.NoContent();
        }

        private async Task<IResult> HandleTokenRotationAsync(HttpContext ctx, AuthFlowContext flow, EffectiveAuthResponse response)
        {
            var refreshToken = _refreshTokenResolver.Resolve(ctx);
            if (refreshToken is null)
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;

            var result = await _tokenRotation.RotateAsync(
                flow,
                new RefreshTokenRotationContext<TUserId>
                {
                    RefreshToken = refreshToken,
                    Now = DateTimeOffset.UtcNow
                },
                ctx.RequestAborted);

            if (!result.IsSuccess)
            {
                WriteRefreshHeader(ctx, flow, RefreshOutcome.ReauthRequired);
                return Results.Unauthorized();
            }

            _credentialWriter.Write(ctx, CredentialKind.AccessToken, result.AccessToken.Token);
            _credentialWriter.Write(ctx, CredentialKind.RefreshToken, result.RefreshToken.Token);
            WriteRefreshHeader(ctx, flow, RefreshOutcome.Rotated);

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
