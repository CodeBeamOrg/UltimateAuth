using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IRefreshTokenResolver<TUserId>
    {
        Task<ResolvedRefreshSession<TUserId>?> ResolveAsync(string? tenantId, string refreshToken, DateTimeOffset now, CancellationToken ct = default);
    }

}
