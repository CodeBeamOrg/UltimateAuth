namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class PkceAuthorizeRequest
{
    public string CodeChallenge { get; init; } = default!;
    public string ChallengeMethod { get; init; } = default!;
    public string? RedirectUri { get; init; }
}
