using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services
{
    public interface IRefreshTokenRotationService
    {
        Task<RefreshTokenRotationExecution> RotateAsync(AuthFlowContext flow, RefreshTokenRotationContext context, CancellationToken ct = default);
    }
}
