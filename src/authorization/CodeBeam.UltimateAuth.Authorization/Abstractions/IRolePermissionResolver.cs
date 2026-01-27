using CodeBeam.UltimateAuth.Authorization.Domain;

namespace CodeBeam.UltimateAuth.Authorization
{
    public interface IRolePermissionResolver
    {
        Task<IReadOnlyCollection<Permission>> ResolveAsync(string? tenantId, IEnumerable<string> roles, CancellationToken ct = default);
    }
}
