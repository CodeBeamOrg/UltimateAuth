namespace CodeBeam.UltimateAuth.Core.Domain;

public enum RefreshOutcome
{
    Success,        // minimal transport
    NoOp,
    Touched,
    Rotated,
    ReauthRequired
}
