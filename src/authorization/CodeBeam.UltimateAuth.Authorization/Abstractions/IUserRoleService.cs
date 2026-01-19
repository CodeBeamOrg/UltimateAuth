using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization
{
    public interface IUserRoleService
    {
        Task AssignAsync(string? tenantId, UserKey userKey, string role, CancellationToken ct = default);

        Task RemoveAsync(string? tenantId, UserKey userKey, string role, CancellationToken ct = default);

        Task<IReadOnlyCollection<string>> GetRolesAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
    }
}
