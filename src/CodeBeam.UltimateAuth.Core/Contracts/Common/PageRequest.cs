using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public record PageRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 0;

    public string? SortBy { get; init; }
    public bool Descending { get; init; }

    public PageRequest Normalize(IUAuthPaginationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var page = Math.Max(1, PageNumber);
        var size = PageSize <= 0 ? policy.DefaultPageSize : PageSize;

        size = Math.Min(size, policy.MaxPageSize);

        return this with
        {
            PageNumber = page,
            PageSize = size
        };
    }
}
