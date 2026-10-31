using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record SandboxQuery
{
    public string? Search { get; init; }
    public SandboxStatus? Status { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; }
    public bool Descending { get; init; } = true;
}
