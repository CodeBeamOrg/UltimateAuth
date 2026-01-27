using CodeBeam.UltimateAuth.Authorization.Domain;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    public sealed class DefaultRolePermissionResolver : IRolePermissionResolver
    {
        private static readonly IReadOnlyDictionary<string, Permission[]> _map
            = new Dictionary<string, Permission[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["admin"] = new[]
                {
                new Permission("*")
                },
                ["user"] = new[]
                {
                new Permission("profile.read"),
                new Permission("profile.update")
                }
            };

        public Task<IReadOnlyCollection<Permission>> ResolveAsync(string? tenantId, IEnumerable<string> roles, CancellationToken ct = default)
        {
            var result = new List<Permission>();

            foreach (var role in roles)
            {
                if (_map.TryGetValue(role, out var perms))
                    result.AddRange(perms);
            }

            return Task.FromResult<IReadOnlyCollection<Permission>>(result);
        }
    }

}
