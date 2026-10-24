namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record AdminSandboxPage(
    IReadOnlyList<AdminSandboxItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
