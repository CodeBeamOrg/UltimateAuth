using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Contracts;

public sealed record RefreshResult
{
    public bool Ok { get; init; }
    public int Status { get; init; }
    public RefreshOutcome Outcome { get; init; }
}
