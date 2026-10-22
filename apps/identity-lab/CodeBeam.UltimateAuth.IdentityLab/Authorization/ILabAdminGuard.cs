using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public interface ILabAdminGuard
{
    Task<bool> IsAdminAsync(AccessContext context, CancellationToken ct = default);
    Task EnsureAdminAsync(AccessContext context, CancellationToken ct = default);
}
