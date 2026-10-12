using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public interface IUserSummaryQueryStore
{
    Task<PagedResult<UserSummary>> QueryAsync(UserQuery query, CancellationToken ct = default);
}
