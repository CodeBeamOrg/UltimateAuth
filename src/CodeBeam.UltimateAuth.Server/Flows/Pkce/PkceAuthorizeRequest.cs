using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class PkceAuthorizeRequest
{
    public string CodeChallenge { get; init; } = default!;
    public string ChallengeMethod { get; init; } = default!;
    public string? RedirectUri { get; init; }
    public required DeviceContext Device { get; init; }
}
