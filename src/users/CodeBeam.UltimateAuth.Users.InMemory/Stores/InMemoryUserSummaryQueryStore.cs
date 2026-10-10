using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSummaryQueryStore : IUserSummaryQueryStore
{
    private readonly IUserLifecycleStore _lifecycles;
    private readonly IUserProfileStore _profiles;
    private readonly IUserIdentifierStore _identifiers;
    private readonly IUAuthPaginationPolicy _pagination;

    public InMemoryUserSummaryQueryStore(IUserLifecycleStore lifecycles, IUserProfileStore profiles, IUserIdentifierStore identifiers, IUAuthPaginationPolicy pagination)
    {
        _lifecycles = lifecycles;
        _profiles = profiles;
        _identifiers = identifiers;
        _pagination = pagination;
    }

    public async Task<PagedResult<UserSummary>> QueryAsync(UserQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = query.Normalize(_pagination);
        var profileKey = query.ProfileKey ?? ProfileKey.Default;

        const int batchSize = 500;

        var summaries = new List<UserSummary>();
        var page = 1;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var result = await _lifecycles.QueryAsync(
                new UserLifecycleQuery
                {
                    PageNumber = page,
                    PageSize = batchSize,
                    SortBy = nameof(UserLifecycle.CreatedAt),
                    Status = query.Status,
                    IncludeDeleted = query.IncludeDeleted
                },
                ct);

            if (result.Items.Count == 0)
                break;

            var keys = result.Items
                .Select(x => x.UserKey)
                .ToList();

            var profiles = await _profiles.GetByUsersAsync(
                keys, profileKey, ct);

            var identifiers = await _identifiers.GetByUsersAsync(
                keys, ct);

            var profileMap = profiles.ToDictionary(x => x.UserKey);

            var identifierGroups = identifiers
                .GroupBy(x => x.UserKey)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var lifecycle in result.Items)
            {
                profileMap.TryGetValue(
                    lifecycle.UserKey, out var profile);

                identifierGroups.TryGetValue(
                    lifecycle.UserKey, out var ids);

                summaries.Add(new UserSummary
                {
                    UserKey = lifecycle.UserKey,
                    DisplayName = profile?.DisplayName,

                    UserName = PrimaryValue(
                        ids, UserIdentifierType.Username),

                    PrimaryEmail = PrimaryValue(
                        ids, UserIdentifierType.Email),

                    PrimaryPhone = PrimaryValue(
                        ids, UserIdentifierType.Phone),

                    Status = lifecycle.Status,
                    CreatedAt = lifecycle.CreatedAt
                });
            }

            if (!result.HasNext)
                break;

            page++;
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var userKey = UserKey.FromString(search);

            summaries = summaries.Where(x =>
                x.UserKey == userKey ||
                Contains(x.DisplayName, search) ||
                Contains(x.UserName, search) ||
                Contains(x.PrimaryEmail, search) ||
                Contains(x.PrimaryPhone, search)
            ).ToList();
        }

        IEnumerable<UserSummary> ordered = query.SortBy switch
        {
            nameof(UserSummary.DisplayName) =>
                query.Descending
                    ? summaries
                        .OrderByDescending(x => x.DisplayName)
                        .ThenBy(x => x.UserKey.Value)
                    : summaries
                        .OrderBy(x => x.DisplayName)
                        .ThenBy(x => x.UserKey.Value),

            nameof(UserSummary.CreatedAt) =>
                query.Descending
                    ? summaries
                        .OrderByDescending(x => x.CreatedAt)
                        .ThenBy(x => x.UserKey.Value)
                    : summaries
                        .OrderBy(x => x.CreatedAt)
                        .ThenBy(x => x.UserKey.Value),

            _ => summaries
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.UserKey.Value)
        };

        var total = summaries.Count;

        var offset =
            ((long)normalized.PageNumber - 1) *
            normalized.PageSize;

        var items = offset > int.MaxValue
            ? new List<UserSummary>()
            : ordered
                .Skip((int)offset)
                .Take(normalized.PageSize)
                .ToList();

        return new PagedResult<UserSummary>(
            items,
            total,
            normalized.PageNumber,
            normalized.PageSize,
            query.SortBy,
            query.Descending);
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(
            search,
            StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    private static string? PrimaryValue(
        IReadOnlyList<UserIdentifier>? identifiers,
        UserIdentifierType type)
    {
        return identifiers?
            .Where(x => x.Type == type && x.IsPrimary)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}
