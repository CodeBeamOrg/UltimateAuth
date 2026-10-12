namespace CodeBeam.UltimateAuth.Server.Flows;

public sealed class SessionTouchPolicy
{
    public TimeSpan? TouchInterval { get; init; }
    public TimeSpan Lifetime { get; init; }
    public bool SlidingExpiration { get; init; }
    public TimeSpan? IdleTimeout { get; init; }
}
