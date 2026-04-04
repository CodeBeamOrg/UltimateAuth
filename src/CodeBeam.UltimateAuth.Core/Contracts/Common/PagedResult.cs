namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public string? SortBy { get; init; }
    public bool Descending { get; init; }

    public bool HasNext => PageNumber * PageSize < TotalCount;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize, string? sortBy, bool descending)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        SortBy = sortBy;
        Descending = descending;
    }
}
