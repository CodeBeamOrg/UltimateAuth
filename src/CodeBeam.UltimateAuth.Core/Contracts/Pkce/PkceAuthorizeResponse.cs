namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class PkceAuthorizeResponse
{
    public string AuthorizationCode { get; init; } = default!;
    public int ExpiresIn { get; init; }
}
