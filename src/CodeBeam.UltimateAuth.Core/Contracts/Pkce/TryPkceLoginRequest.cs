namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class TryPkceLoginRequest
{
    public string AuthorizationCode { get; init; } = string.Empty;
    public string CodeVerifier { get; init; } = string.Empty;
    public string? Identifier { get; init; }
    public string? Secret { get; init; }
    public string? ReturnUrl { get; init; }
    public string? HubSessionId { get; init; }
}
