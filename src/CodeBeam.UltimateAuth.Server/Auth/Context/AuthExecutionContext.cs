using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed record AuthExecutionContext
{
    public required UAuthClientProfile? EffectiveClientProfile { get; init; }
}
