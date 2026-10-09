using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserSummaryQueryStore<TDbContext> : IUserSummaryQueryStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;
    private readonly IUAuthPaginationPolicy _pagination;

    public EfCoreUserSummaryQueryStore(TDbContext db, TenantExecutionContext tenant, IUAuthPaginationPolicy pagination)
    {
        _db = db;
        _tenant = tenant.Tenant;
        _pagination = pagination;
    }

    public async Task<PagedResult<UserSummary>> QueryAsync(UserQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize(_pagination);
        var profileKey = query.ProfileKey ?? ProfileKey.Default;

        var lifecycles = _db.Set<UserLifecycleProjection>()
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant);

        if (!query.IncludeDeleted)
        {
            lifecycles = lifecycles.Where(x => x.DeletedAt == null);
        }

        if (query.Status.HasValue)
        {
            lifecycles = lifecycles.Where(x => x.Status == query.Status.Value);
        }

        var profiles = _db.Set<UserProfileProjection>()
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.ProfileKey == profileKey &&
                x.DeletedAt == null);

        var identifiers = _db.Set<UserIdentifierProjection>()
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.DeletedAt == null &&
                x.IsPrimary);

        var summaries = lifecycles.Select(l => new UserSummary
        {
            UserKey = l.UserKey,
            Status = l.Status,
            CreatedAt = l.CreatedAt,

            DisplayName = profiles
                .Where(p => p.UserKey == l.UserKey)
                .Select(p => p.DisplayName)
                .FirstOrDefault(),

            UserName = identifiers
                .Where(i =>
                    i.UserKey == l.UserKey &&
                    i.Type == UserIdentifierType.Username)
                .OrderBy(i => i.CreatedAt)
                .ThenBy(i => i.Id)
                .Select(i => i.Value)
                .FirstOrDefault(),

            PrimaryEmail = identifiers
                .Where(i =>
                    i.UserKey == l.UserKey &&
                    i.Type == UserIdentifierType.Email)
                .OrderBy(i => i.CreatedAt)
                .ThenBy(i => i.Id)
                .Select(i => i.Value)
                .FirstOrDefault(),

            PrimaryPhone = identifiers
                .Where(i =>
                    i.UserKey == l.UserKey &&
                    i.Type == UserIdentifierType.Phone)
                .OrderBy(i => i.CreatedAt)
                .ThenBy(i => i.Id)
                .Select(i => i.Value)
                .FirstOrDefault()
        });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            // Escaped substring matching.
            // Database collation determines case sensitivity.
            var pattern = "%" + EscapeLike(search) + "%";
            var userKey = UserKey.FromString(search);

            summaries = summaries.Where(x =>
                x.UserKey == userKey ||

                (x.DisplayName != null &&
                    EF.Functions.Like(
                        x.DisplayName, pattern, "\\")) ||
                (x.UserName != null &&
                    EF.Functions.Like(
                        x.UserName, pattern, "\\")) ||
                (x.PrimaryEmail != null &&
                    EF.Functions.Like(
                        x.PrimaryEmail, pattern, "\\")) ||
                (x.PrimaryPhone != null &&
                    EF.Functions.Like(
                        x.PrimaryPhone, pattern, "\\")));
        }

        var total = await summaries.CountAsync(ct);

        var ordered = query.SortBy switch
        {
            nameof(UserSummary.DisplayName) =>
                query.Descending
                    ? summaries.OrderByDescending(x => x.DisplayName).ThenBy(x => x.CreatedAt)
                    : summaries.OrderBy(x => x.DisplayName).ThenBy(x => x.CreatedAt),

            nameof(UserSummary.UserName) =>
                query.Descending
                    ? summaries.OrderByDescending(x => x.UserName).ThenBy(x => x.CreatedAt)
                    : summaries.OrderBy(x => x.UserName).ThenBy(x => x.CreatedAt),

            nameof(UserSummary.PrimaryEmail) =>
                query.Descending
                    ? summaries.OrderByDescending(x => x.PrimaryEmail).ThenBy(x => x.CreatedAt)
                    : summaries.OrderBy(x => x.PrimaryEmail).ThenBy(x => x.CreatedAt),

            nameof(UserSummary.PrimaryPhone) =>
                query.Descending
                    ? summaries.OrderByDescending(x => x.PrimaryPhone).ThenBy(x => x.CreatedAt)
                    : summaries.OrderBy(x => x.PrimaryPhone).ThenBy(x => x.CreatedAt),

            nameof(UserSummary.CreatedAt) =>
                query.Descending
                    ? summaries.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.UserKey)
                    : summaries.OrderBy(x => x.CreatedAt).ThenBy(x => x.UserKey),

            _ => query.Descending
                ? summaries.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.UserKey)
                : summaries.OrderBy(x => x.CreatedAt).ThenBy(x => x.UserKey)
        };

        var offset =
            ((long)normalized.PageNumber - 1) *
            normalized.PageSize;

        if (offset > int.MaxValue)
        {
            return new PagedResult<UserSummary>(
                Array.Empty<UserSummary>(),
                total,
                normalized.PageNumber,
                normalized.PageSize,
                query.SortBy,
                query.Descending);
        }

        var items = await ordered
            .Skip((int)offset)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserSummary>(
            items,
            total,
            normalized.PageNumber,
            normalized.PageSize,
            query.SortBy,
            query.Descending);
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }
}
