using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    internal sealed class InMemoryUserClaimsProvider<TUserId> : IUserClaimsProvider<TUserId> where TUserId : notnull
    {
        private readonly InMemoryUserStore<TUserId> _store;

        public InMemoryUserClaimsProvider(InMemoryUserStore<TUserId> store)
        {
            _store = store;
        }

        public async Task<ClaimsSnapshot> GetClaimsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            return ClaimsSnapshot.Empty;
        }
    }
}
