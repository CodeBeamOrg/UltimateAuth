namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class PkceAuthorizeResponse
{
    public required string AuthorizationCode { get; init; }
    public int ExpiresIn { get; init; }
}
