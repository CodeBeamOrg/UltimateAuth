using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.InMemory
{
    internal sealed class InMemoryAuthorizationSeeder : IAuthorizationSeeder
    {
        private readonly IUserRoleStore _roles;
        private readonly IInMemoryUserIdProvider<UserKey> _ids;

        public InMemoryAuthorizationSeeder(IUserRoleStore roles, IInMemoryUserIdProvider<UserKey> ids)
        {
            _roles = roles;
            _ids = ids;
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            var key = _ids.GetAdminUserId();
            await _roles.AssignAsync(null, key, "Admin", ct);
        }
    }
}
