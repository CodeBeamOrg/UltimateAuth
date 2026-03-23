namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed record PkceCredentials
{
    public string AuthorizationCode { get; init; } = default!;
    public string CodeVerifier { get; init; } = default!;
}
