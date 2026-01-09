using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services
{
    public interface IRefreshTokenRotationService<TUserId>
    {
        Task<RefreshTokenRotationExecution<TUserId>> RotateAsync(AuthFlowContext flow, RefreshTokenRotationContext<TUserId> context, CancellationToken ct = default);
    }
}
