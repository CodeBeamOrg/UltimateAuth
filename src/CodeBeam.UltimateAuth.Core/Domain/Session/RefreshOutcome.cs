namespace CodeBeam.UltimateAuth.Core.Domain
{
    public enum RefreshOutcome
    {
        None,
        NoOp,
        Touched,
        ReauthRequired
    }
}
