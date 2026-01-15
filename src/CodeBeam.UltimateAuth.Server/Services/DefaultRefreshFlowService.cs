using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class DefaultRefreshFlowService : IRefreshFlowService
    {
        private readonly ISessionQueryService _sessionQueries;
        private readonly ISessionTouchService _sessionRefresh;
        private readonly IRefreshTokenRotationService _tokenRotation;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IUserIdConverterResolver _userIdConverterResolver;

        public DefaultRefreshFlowService(
            ISessionQueryService sessionQueries,
            ISessionTouchService sessionRefresh,
            IRefreshTokenRotationService tokenRotation,
            IRefreshTokenStore refreshTokenStore,
            IUserIdConverterResolver userIdConverterResolver)
        {
            _sessionQueries = sessionQueries;
            _sessionRefresh = sessionRefresh;
            _tokenRotation = tokenRotation;
            _refreshTokenStore = refreshTokenStore;
            _userIdConverterResolver = userIdConverterResolver;
        }

        public async Task<RefreshFlowResult> RefreshAsync(AuthFlowContext flow, RefreshFlowRequest request, CancellationToken ct = default)
        {
            return flow.EffectiveMode switch
            {
                UAuthMode.PureOpaque =>
                    await HandleSessionOnlyAsync(flow, request, ct),

                UAuthMode.PureJwt =>
                    await HandleTokenOnlyAsync(flow, request, ct),

                UAuthMode.Hybrid =>
                    await HandleHybridAsync(flow, request, ct),

                UAuthMode.SemiHybrid =>
                    await HandleSemiHybridAsync(flow, request, ct),

                _ => RefreshFlowResult.ReauthRequired()
            };
        }

        private async Task<RefreshFlowResult> HandleSessionOnlyAsync(AuthFlowContext flow, RefreshFlowRequest request, CancellationToken ct)
        {
            if (request.SessionId is null)
                return RefreshFlowResult.ReauthRequired();

            var validation = await _sessionQueries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = flow.TenantId,
                    SessionId = request.SessionId.Value,
                    Now = request.Now,
                    Device = request.Device
                },
                ct);

            if (!validation.IsValid)
                return RefreshFlowResult.ReauthRequired();

            var touchPolicy = new SessionTouchPolicy
            {
                TouchInterval = flow.EffectiveOptions.Options.Session.TouchInterval
            };

            var refresh = await _sessionRefresh.RefreshAsync(validation, touchPolicy, request.TouchMode, request.Now, ct);

            if (!refresh.IsSuccess || refresh.SessionId is null)
                return RefreshFlowResult.ReauthRequired();

            return RefreshFlowResult.Success(refresh.DidTouch ? RefreshOutcome.Touched : RefreshOutcome.NoOp, refresh.SessionId);
        }

        private async Task<RefreshFlowResult> HandleTokenOnlyAsync(AuthFlowContext flow, RefreshFlowRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return RefreshFlowResult.ReauthRequired();

            var rotation = await _tokenRotation.RotateAsync(
                flow,
                new RefreshTokenRotationContext
                {
                    RefreshToken = request.RefreshToken!,
                    Now = request.Now,
                    Device = request.Device
                },
                ct);

            if (!rotation.Result.IsSuccess)
                return RefreshFlowResult.ReauthRequired();

            //if (rotation.Result.RefreshToken is not null)
            //{
            //    var converter = _userIdConverterResolver.GetConverter<TUserId>();

            //    await _refreshTokenStore.StoreAsync(
            //        flow.TenantId,
            //        new StoredRefreshToken<TUserId>
            //        {
            //            TokenHash = rotation.Result.RefreshToken.TokenHash,
            //            UserId = rotation.UserId!,
            //            SessionId = rotation.SessionId!.Value,
            //            ChainId = rotation.ChainId,
            //            ExpiresAt = rotation.Result.RefreshToken.ExpiresAt,
            //            IssuedAt = request.Now
            //        },
            //        ct);
            //}

            return RefreshFlowResult.Success(
                outcome: RefreshOutcome.Rotated,
                accessToken: rotation.Result.AccessToken,
                refreshToken: rotation.Result.RefreshToken);
        }

        private async Task<RefreshFlowResult> HandleHybridAsync(AuthFlowContext flow, RefreshFlowRequest request, CancellationToken ct)
        {
            if (request.SessionId is null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return RefreshFlowResult.ReauthRequired();

            var validation = await _sessionQueries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = flow.TenantId,
                    SessionId = request.SessionId.Value,
                    Now = request.Now,
                    Device = request.Device
                },
                ct);

            if (!validation.IsValid)
                return RefreshFlowResult.ReauthRequired();

            var rotation = await _tokenRotation.RotateAsync(
                flow,
                new RefreshTokenRotationContext
                {
                    RefreshToken = request.RefreshToken!,
                    Now = request.Now,
                    Device = request.Device,
                    ExpectedSessionId = request.SessionId.Value
                },
                ct);

            if (!rotation.Result.IsSuccess)
                return RefreshFlowResult.ReauthRequired();

            var touchPolicy = new SessionTouchPolicy
            {
                TouchInterval = flow.EffectiveOptions.Options.Session.TouchInterval
            };

            var refresh = await _sessionRefresh.RefreshAsync(validation, touchPolicy, request.TouchMode, request.Now, ct);

            if (!refresh.IsSuccess || refresh.SessionId is null)
                return RefreshFlowResult.ReauthRequired();

            //await StoreRefreshTokenAsync(flow, rotation, request.Now, ct);

            return RefreshFlowResult.Success(
                outcome: RefreshOutcome.Rotated,
                sessionId: refresh.SessionId,
                accessToken: rotation.Result.AccessToken,
                refreshToken: rotation.Result.RefreshToken);
        }

        private async Task<RefreshFlowResult> HandleSemiHybridAsync(AuthFlowContext flow, RefreshFlowRequest request, CancellationToken ct)
        {
            if (request.SessionId is null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return RefreshFlowResult.ReauthRequired();

            var validation = await _sessionQueries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = flow.TenantId,
                    SessionId = request.SessionId.Value,
                    Now = request.Now,
                    Device = request.Device
                },
                ct);

            if (!validation.IsValid)
                return RefreshFlowResult.ReauthRequired();

            var rotation = await _tokenRotation.RotateAsync(
                flow,
                new RefreshTokenRotationContext
                {
                    RefreshToken = request.RefreshToken!,
                    Now = request.Now,
                    Device = request.Device,
                    ExpectedSessionId = request.SessionId.Value
                },
                ct);

            if (!rotation.Result.IsSuccess)
                return RefreshFlowResult.ReauthRequired();

            // ❗ NO SESSION TOUCH HERE
            // Session lifetime is fixed in SemiHybrid

            //await StoreRefreshTokenAsync(flow, rotation, request.Now, ct);

            return RefreshFlowResult.Success(
                outcome: RefreshOutcome.Rotated,
                sessionId: request.SessionId.Value,
                accessToken: rotation.Result.AccessToken,
                refreshToken: rotation.Result.RefreshToken);
        }

        //private async Task StoreRefreshTokenAsync(AuthFlowContext flow, RefreshTokenRotationExecution<TUserId> rotation, DateTimeOffset now, CancellationToken ct)
        //{
        //    if (rotation.Result.RefreshToken is null)
        //        return;

        //    await _refreshTokenStore.StoreAsync(
        //        flow.TenantId,
        //        new StoredRefreshToken<TUserId>
        //        {
        //            TokenHash = rotation.Result.RefreshToken.TokenHash,
        //            UserId = rotation.UserId!,
        //            SessionId = rotation.SessionId!.Value,
        //            ChainId = rotation.ChainId,
        //            ExpiresAt = rotation.Result.RefreshToken.ExpiresAt,
        //            IssuedAt = now
        //        },
        //        ct);
        //}

    }
}
