using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public interface IUserSummaryQueryStore
{
    Task<PagedResult<UserSummary>> QueryAsync(UserQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummary>> GetByKeysAsync(IReadOnlyCollection<UserKey> userKeys, CancellationToken ct = default);
}
