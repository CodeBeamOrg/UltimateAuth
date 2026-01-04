using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultRefreshEndpointHandler<TUserId> : IRefreshEndpointHandler where TUserId : notnull
    {
        private readonly ISessionContextAccessor _sessionContextAccessor;
        private readonly IAuthFlowContextFactory _authFlowContextFactory;
        private readonly IAuthResponseResolver _authResponseResolver;
        private readonly ISessionRefreshService<TUserId> _sessionRefresh;
        private readonly IRefreshTokenRotationService<TUserId> _tokenRotation;
        private readonly ICredentialResponseWriter _credentialWriter;
        private readonly IRefreshResponseWriter _refreshWriter;
        private readonly ISessionQueryService<TUserId> _sessionQueries;
        private readonly IRefreshTokenResolver _refreshTokenResolver;

        public DefaultRefreshEndpointHandler(
            ISessionContextAccessor sessionContextAccessor,
            IAuthFlowContextFactory authFlowContextFactory,
            IAuthResponseResolver authResponseResolver,
            ISessionRefreshService<TUserId> sessionRefresh,
            IRefreshTokenRotationService<TUserId> tokenRotation,
            ICredentialResponseWriter credentialWriter,
            IRefreshResponseWriter refreshWriter,
            ISessionQueryService<TUserId> sessionQueries,
            IRefreshTokenResolver refreshTokenResolver)
        {
            _sessionContextAccessor = sessionContextAccessor;
            _authFlowContextFactory = authFlowContextFactory;
            _authResponseResolver = authResponseResolver;
            _sessionRefresh = sessionRefresh;
            _tokenRotation = tokenRotation;
            _credentialWriter = credentialWriter;
            _refreshWriter = refreshWriter;
            _sessionQueries = sessionQueries;
            _refreshTokenResolver = refreshTokenResolver;
        }

        public async Task<IResult> RefreshAsync(HttpContext ctx)
        {
            var flowContext = _authFlowContextFactory.Create(ctx, AuthFlowType.Refresh);
            var authResponse = _authResponseResolver.Resolve(flowContext);
            var decision = RefreshDecisionResolver.Resolve(flowContext.EffectiveMode);

            return decision switch
            {
                RefreshDecision.SessionTouch => await HandleSessionTouchAsync(ctx, flowContext, authResponse),
                RefreshDecision.TokenRotation => await HandleTokenRotationAsync(ctx, flowContext, authResponse),

                _ => Results.StatusCode(StatusCodes.Status409Conflict)
            };
        }

        private async Task<IResult> HandleSessionTouchAsync(HttpContext ctx, AuthFlowContext flow, EffectiveAuthResponse response)
        {
            var sessionCtx = _sessionContextAccessor.Current;
            if (sessionCtx?.SessionId is null)
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            var validation = await _sessionQueries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = sessionCtx.TenantId,
                    SessionId = sessionCtx.SessionId.Value,
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

            _credentialWriter.Write(ctx, result.PrimaryToken.Value, response.SessionIdDelivery);
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

            _credentialWriter.Write(ctx, result.AccessToken.Token, response.AccessTokenDelivery);
            _credentialWriter.Write(ctx, result.RefreshToken.Token, response.RefreshTokenDelivery);
            WriteRefreshHeader(ctx, flow, RefreshOutcome.Rotated);

            return Results.NoContent();
        }

        private void WriteRefreshHeader(HttpContext ctx, AuthFlowContext flow, RefreshOutcome outcome)
        {
            if (!flow.ServerOptions.Options.Diagnostics.EnableRefreshHeaders)
                return;

            _refreshWriter.Write(ctx, outcome);
        }

        // TODO: Add token refresh support
        //public async Task<IResult> RefreshAsync(HttpContext ctx)
        //{
        //    var options = _effectiveOptions.Get(ctx, AuthFlowType.Refresh);

        //    var decision = RefreshDecisionResolver.Resolve(options.Options);
        //    if (decision != RefreshDecision.SessionOnly)
        //    {
        //        // Endpoint exists, but this mode does not support session refresh
        //        return Results.StatusCode(StatusCodes.Status409Conflict);
        //    }

        //    var sessionContext = _sessionContextAccessor.Current;
        //    if (sessionContext?.SessionId is null)
        //        return Results.Unauthorized();

        //    var now = DateTimeOffset.UtcNow;
        //    var validation = await _sessionQueries.ValidateSessionAsync(
        //        new SessionValidationContext
        //        {
        //            TenantId = sessionContext.TenantId,
        //            SessionId = (AuthSessionId)sessionContext.SessionId,
        //            Now = now,
        //            Device = DeviceInfoFactory.FromHttpContext(ctx)
        //        },
        //        ctx.RequestAborted);


        //    if (!validation.IsValid)
        //    {
        //        if (options.Options.Diagnostics.EnableRefreshHeaders)
        //            _refreshResponseWriter.Write(ctx, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }


        //    var refreshResult = await _sessionRefresh.RefreshAsync(validation, now, ctx.RequestAborted);

        //    RefreshOutcome outcome;

        //    if (!refreshResult.IsSuccess || refreshResult.PrimaryToken is null)
        //    {
        //        outcome = RefreshOutcome.ReauthRequired;

        //        if (options.Options.Diagnostics.EnableRefreshHeaders)
        //            _refreshResponseWriter.Write(ctx, outcome);

        //        return Results.Unauthorized();
        //    }

        //    _credentialResponseWriter.Write(ctx, refreshResult.PrimaryToken.Value, options.AuthResponse.SessionIdDelivery);

        //    outcome = refreshResult.DidTouch
        //        ? RefreshOutcome.Touched
        //        : RefreshOutcome.NoOp;

        //    if (options.Options.Diagnostics.EnableRefreshHeaders)
        //        _refreshResponseWriter.Write(ctx, outcome);

        //    return Results.NoContent();
        //}
    }
}
