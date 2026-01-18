using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    internal sealed class InMemoryUserClaimsProvider : IUserClaimsProvider<UserKey>
    {
        private readonly InMemoryUserStore _store;

        public InMemoryUserClaimsProvider(InMemoryUserStore store)
        {
            _store = store;
        }

        public async Task<ClaimsSnapshot> GetClaimsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            return ClaimsSnapshot.Empty;
        }
    }
}
