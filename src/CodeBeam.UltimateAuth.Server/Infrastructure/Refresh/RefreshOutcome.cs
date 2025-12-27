namespace CodeBeam.UltimateAuth.Server.Infrastructure.Internal
{
    public enum RefreshOutcome
    {
        NoOp,
        Touched,
        ReauthRequired
    }
}
