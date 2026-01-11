namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class PkceLoginRequest
{
    public string AuthorizationCode { get; init; } = default!;
    public string CodeVerifier { get; init; } = default!;
    public string ReturnUrl { get; init; } = default!;

    public string Identifier { get; init; } = default!;
    public string Secret { get; init; } = default!;

    public string? TenantId { get; init; }
}
