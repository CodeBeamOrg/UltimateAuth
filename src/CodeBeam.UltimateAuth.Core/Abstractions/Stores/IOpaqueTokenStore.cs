using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IOpaqueTokenStore
    {
        Task<OpaqueTokenRecord?> FindByHashAsync(
            string tokenHash,
            CancellationToken ct = default);
    }
}
