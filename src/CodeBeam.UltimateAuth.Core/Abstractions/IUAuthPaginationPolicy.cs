namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IUAuthPaginationPolicy
{
    int DefaultPageSize { get; }
    int MaxPageSize { get; }
}
