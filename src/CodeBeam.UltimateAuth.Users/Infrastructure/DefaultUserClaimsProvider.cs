using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class DefaultUserClaimsProvider<TUserId> : IUserClaimsProvider<TUserId>
    {
        public Task<ClaimsSnapshot> GetClaimsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            // Framework default: no claims
            // EF / external providers override this
            return Task.FromResult(ClaimsSnapshot.Empty);
        }
    }
}
