using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services
{
    public interface IRefreshFlowService<TUserId>
    {
        Task<RefreshFlowResult> RefreshAsync(AuthFlowContext flow, RefreshFlowRequest request,
            CancellationToken ct = default);
    }
}
