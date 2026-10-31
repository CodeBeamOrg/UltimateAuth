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


        IOrderedEnumerable<UserSummary> OrderByText(Func<UserSummary, string?> selector)
        {
            return query.Descending
                ? summaries.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase)
                : summaries.OrderBy(selector, StringComparer.OrdinalIgnoreCase);
        }

        IOrderedEnumerable<UserSummary> OrderByCreatedAt()
        {
            return query.Descending
                ? summaries.OrderByDescending(x => x.CreatedAt)
                : summaries.OrderBy(x => x.CreatedAt);
        }

        IOrderedEnumerable<UserSummary> ordered = query.SortBy switch
        {
            nameof(UserSummary.DisplayName) =>
                OrderByText(x => x.DisplayName)
                    .ThenBy(x => x.CreatedAt)
                    .ThenBy(x => x.UserKey.Value),

            nameof(UserSummary.UserName) =>
                OrderByText(x => x.UserName)
                    .ThenBy(x => x.CreatedAt)
                    .ThenBy(x => x.UserKey.Value),

            nameof(UserSummary.PrimaryEmail) =>
                OrderByText(x => x.PrimaryEmail)
                    .ThenBy(x => x.CreatedAt)
                    .ThenBy(x => x.UserKey.Value),

            nameof(UserSummary.PrimaryPhone) =>
                OrderByText(x => x.PrimaryPhone)
                    .ThenBy(x => x.CreatedAt)
                    .ThenBy(x => x.UserKey.Value),

            _ => OrderByCreatedAt()
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

    public async Task<IReadOnlyList<UserSummary>> GetByKeysAsync(IReadOnlyCollection<UserKey> userKeys, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userKeys);
        ct.ThrowIfCancellationRequested();

        var keys = userKeys.Distinct().ToArray();

        if (keys.Length == 0)
            return Array.Empty<UserSummary>();

        var requested = keys.ToHashSet();

        // Reuse the existing lifecycle query to obtain active users.
        var lifecycles = new List<UserLifecycle>();
        var page = 1;

        const int pageSize = 500;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var result = await _lifecycles.QueryAsync(
                new UserLifecycleQuery
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    IncludeDeleted = false
                },
                ct);

            lifecycles.AddRange(result.Items.Where(x => requested.Contains(x.UserKey)));

            if (!result.HasNext)
                break;

            page++;
        }

        if (lifecycles.Count == 0)
            return Array.Empty<UserSummary>();

        var foundKeys = lifecycles.Select(x => x.UserKey).ToArray();

        var profiles = await _profiles.GetByUsersAsync(foundKeys, ProfileKey.Default, ct);
        var identifiers = await _identifiers.GetByUsersAsync(foundKeys, ct);
        var profileMap = profiles.ToDictionary(x => x.UserKey);

        var identifierGroups = identifiers
            .GroupBy(x => x.UserKey)
            .ToDictionary(x => x.Key, x => x.ToList());

        var summaries = new List<UserSummary>(lifecycles.Count);

        foreach (var lifecycle in lifecycles)
        {
            profileMap.TryGetValue(lifecycle.UserKey, out var profile);

            identifierGroups.TryGetValue(lifecycle.UserKey, out var ids);

            summaries.Add(new UserSummary
            {
                UserKey = lifecycle.UserKey,
                Status = lifecycle.Status,
                CreatedAt = lifecycle.CreatedAt,

                DisplayName = profile?.DisplayName,
                UserName = PrimaryValue(ids, UserIdentifierType.Username),
                PrimaryEmail = PrimaryValue(ids, UserIdentifierType.Email),
                PrimaryPhone = PrimaryValue(ids, UserIdentifierType.Phone)
            });
        }

        return summaries
            .OrderBy(x => x.UserKey.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    private static string? PrimaryValue(IReadOnlyList<UserIdentifier>? identifiers, UserIdentifierType type)
    {
        return identifiers?
            .Where(x => x.Type == type && x.IsPrimary)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}
