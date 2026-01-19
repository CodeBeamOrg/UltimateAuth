using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    internal sealed class DefaultUserRoleService : IUserRoleService
    {
        private readonly IUserRoleStore _store;

        public DefaultUserRoleService(IUserRoleStore store)
        {
            _store = store;
        }

        public Task AssignAsync(string? tenantId, UserKey userKey, string role, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty.", nameof(role));

            return _store.AssignAsync(tenantId, userKey, role, ct);
        }

        public Task RemoveAsync(string? tenantId, UserKey userKey, string role, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty.", nameof(role));

            return _store.RemoveAsync(tenantId, userKey, role, ct);
        }

        public Task<IReadOnlyCollection<string>> GetRolesAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            return _store.GetRolesAsync(tenantId, userKey, ct);
        }
    }
}
