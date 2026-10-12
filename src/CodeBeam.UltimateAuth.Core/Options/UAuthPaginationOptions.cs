using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Options;

public sealed class UAuthPaginationOptions : IUAuthPaginationPolicy
{
    public const int DefaultPageSizeValue = 250;
    public const int DefaultMaxPageSizeValue = 1000;

    public int DefaultPageSize { get; set; } = DefaultPageSizeValue;

    public int MaxPageSize { get; set; } = DefaultMaxPageSizeValue;

    internal UAuthPaginationOptions Clone() => new()
    {
        DefaultPageSize = DefaultPageSize,
        MaxPageSize = MaxPageSize
    };
}
