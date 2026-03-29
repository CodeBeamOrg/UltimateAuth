namespace CodeBeam.UltimateAuth.Core.Contracts;

public record PageRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 250;

    public string? SortBy { get; init; }
    public bool Descending { get; init; }

    public int MaxPageSize { get; init; } = 1000;

    public PageRequest Normalize()
    {
        var page = PageNumber <= 0 ? 1 : PageNumber;
        var size = PageSize <= 0 ? 250 : PageSize;

        if (size > MaxPageSize)
            size = MaxPageSize;

        return new PageRequest
        {
            PageNumber = page,
            PageSize = size,
            SortBy = SortBy,
            Descending = Descending,
            MaxPageSize = MaxPageSize
        };
    }
}
