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
        private readonly IRefreshFlowService<TUserId> _refreshFlow;
        private readonly ICredentialResponseWriter _credentialWriter;
        private readonly IRefreshResponseWriter _refreshWriter;
        private readonly IRefreshTokenResolver _refreshTokenResolver;
        private readonly IRefreshResponsePolicy _refreshPolicy;

        public DefaultRefreshEndpointHandler(
            IAuthFlowContextAccessor authContext,
            IRefreshFlowService<TUserId> refreshFlow,
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

            var request = new RefreshFlowRequest
            {
                SessionId = flow.SessionId,
                RefreshToken = _refreshTokenResolver.Resolve(ctx),
                Device = DeviceInfoFactory.FromHttpContext(ctx),
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
                _credentialWriter.Write(ctx, CredentialKind.AccessToken, result.AccessToken.Token);
            }

            if (_refreshPolicy.WriteRefreshToken(flow) && result.RefreshToken is not null)
            {
                _credentialWriter.Write(ctx, CredentialKind.RefreshToken, result.RefreshToken.Token);
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

        //public async Task<IResult> RefreshAsync(HttpContext ctx)
        //{
        //    var auth = _authContext.Current;
        //    var strategy = RefreshStrategyResolver.Resolve(auth.EffectiveMode);

        //    return strategy switch
        //    {
        //        RefreshStrategy.SessionOnly => await HandleSessionOnlyRefreshAsync(ctx, auth),
        //        RefreshStrategy.TokenOnly => await HandleTokenOnlyRefreshAsync(ctx, auth),
        //        RefreshStrategy.SessionAndToken => await HandleSessionAndTokenRefreshAsync(ctx, auth),
        //        RefreshStrategy.TokenWithSessionCheck => await HandleTokenWithSessionCheckRefreshAsync(ctx, auth),

        //        _ => Results.StatusCode(StatusCodes.Status409Conflict)
        //    };
        //}

        //private async Task<IResult> HandleSessionOnlyRefreshAsync(HttpContext ctx, AuthFlowContext authContext)
        //{
        //    if (authContext.SessionId is null)
        //        return Results.Unauthorized();

        //    var now = DateTimeOffset.UtcNow;
        //    var validation = await _sessionQueries.ValidateSessionAsync(
        //        new SessionValidationContext
        //        {
        //            TenantId = authContext.TenantId,
        //            SessionId = authContext.SessionId.Value,
        //            Now = now,
        //            Device = DeviceInfoFactory.FromHttpContext(ctx)
        //        },
        //        ctx.RequestAborted);

        //    if (!validation.IsValid)
        //    {
        //        WriteRefreshHeader(ctx, authContext, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }

        //    var result = await _sessionRefresh.RefreshAsync(validation, now, ctx.RequestAborted);

        //    if (!result.IsSuccess || result.PrimaryToken is null)
        //    {
        //        WriteRefreshHeader(ctx, authContext, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }

        //    _credentialWriter.Write(ctx, CredentialKind.Session, result.PrimaryToken.Value);
        //    WriteRefreshHeader(ctx, authContext, result.DidTouch ? RefreshOutcome.Touched : RefreshOutcome.NoOp);

        //    return Results.NoContent();
        //}

        //private async Task<IResult> HandleSessionAndTokenRefreshAsync(HttpContext ctx, AuthFlowContext authContext)
        //{
        //    // 1️⃣ ZORUNLU CREDENTIALS
        //    if (authContext.SessionId is null)
        //        return Results.Unauthorized();

        //    var refreshToken = _refreshTokenResolver.Resolve(ctx);
        //    if (refreshToken is null)
        //        return Results.Unauthorized();

        //    var now = DateTimeOffset.UtcNow;
        //    var device = DeviceInfoFactory.FromHttpContext(ctx);

        //    // 2️⃣ SESSION VALIDATION (AUTHORITY ANCHOR)
        //    var sessionValidation = await _sessionQueries.ValidateSessionAsync(
        //        new SessionValidationContext
        //        {
        //            TenantId = authContext.TenantId,
        //            SessionId = authContext.SessionId.Value,
        //            Now = now,
        //            Device = device
        //        },
        //        ctx.RequestAborted);

        //    if (!sessionValidation.IsValid)
        //    {
        //        WriteRefreshHeader(ctx, authContext, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }

        //    // 3️⃣ TOKEN ROTATION (CONTINUITY PROOF)
        //    var rotationResult = await _tokenRotation.RotateAsync(
        //        authContext,
        //        new RefreshTokenRotationContext<TUserId>
        //        {
        //            RefreshToken = refreshToken,
        //            Now = now,
        //            Device = device,
        //            SessionId = authContext.SessionId.Value // 🔐 kritik bağ
        //        },
        //        ctx.RequestAborted);

        //    if (!rotationResult.IsSuccess)
        //    {
        //        WriteRefreshHeader(ctx, authContext, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }

        //    // 4️⃣ SESSION TOUCH (SLIDING EXPIRATION)
        //    var touchResult = await _sessionRefresh.RefreshAsync(
        //        sessionValidation,
        //        now,
        //        ctx.RequestAborted);

        //    if (!touchResult.IsSuccess || touchResult.PrimaryToken is null)
        //    {
        //        WriteRefreshHeader(ctx, authContext, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }

        //    // 5️⃣ WRITE ALL CREDENTIALS
        //    _credentialWriter.Write(ctx, CredentialKind.Session, touchResult.PrimaryToken.Value);
        //    _credentialWriter.Write(ctx, CredentialKind.AccessToken, rotationResult.AccessToken.Token);
        //    _credentialWriter.Write(ctx, CredentialKind.RefreshToken, rotationResult.RefreshToken.Token);

        //    WriteRefreshHeader(ctx, authContext, RefreshOutcome.Rotated);
        //    return Results.NoContent();
        //}

        //private async Task<IResult> HandleTokenWithSessionCheckRefreshAsync(HttpContext ctx, AuthFlowContext authContext)
        //{
        //    throw new NotImplementedException();
        //}


        //private async Task<IResult> HandleTokenOnlyRefreshAsync(HttpContext ctx, AuthFlowContext authContext)
        //{
        //    var refreshToken = _refreshTokenResolver.Resolve(ctx);
        //    if (refreshToken is null)
        //        return Results.Unauthorized();

        //    var now = DateTimeOffset.UtcNow;

        //    var result = await _tokenRotation.RotateAsync(
        //        authContext,
        //        new RefreshTokenRotationContext<TUserId>
        //        {
        //            RefreshToken = refreshToken,
        //            Now = now
        //        },
        //        ctx.RequestAborted);

        //    if (!result.IsSuccess)
        //    {
        //        WriteRefreshHeader(ctx, authContext, RefreshOutcome.ReauthRequired);
        //        return Results.Unauthorized();
        //    }

        //    _credentialWriter.Write(ctx, CredentialKind.AccessToken, result.AccessToken.Token);
        //    _credentialWriter.Write(ctx, CredentialKind.RefreshToken, result.RefreshToken.Token);
        //    WriteRefreshHeader(ctx, authContext, RefreshOutcome.Rotated);

        //    return Results.NoContent();
        //}

    }
}
