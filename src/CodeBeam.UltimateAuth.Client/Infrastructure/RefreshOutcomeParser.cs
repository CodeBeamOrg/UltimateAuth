using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class RefreshOutcomeParser
{
    public static RefreshOutcome Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return RefreshOutcome.None;

        return value switch
        {
            "no-op" => RefreshOutcome.NoOp,
            "touched" => RefreshOutcome.Touched,
            "reauth-required" => RefreshOutcome.ReauthRequired,
            _ => RefreshOutcome.None
        };
    }
}
