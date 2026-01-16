using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserClaimsProvider<TUserId>
{
    Task<ClaimsSnapshot> GetClaimsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
}
