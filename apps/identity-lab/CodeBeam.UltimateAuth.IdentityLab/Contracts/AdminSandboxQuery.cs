namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record AdminSandboxQuery(
    string? Search = null,
    string? Status = null,
    int Page = 0,
    int PageSize = 10);
