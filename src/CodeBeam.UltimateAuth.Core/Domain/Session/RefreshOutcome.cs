namespace CodeBeam.UltimateAuth.Core.Domain;

public enum RefreshOutcome
{
    Success = 0,        // minimal transport
    NoOp = 10,
    Touched = 20,
    Rotated = 30,
    ReauthRequired = 100
}
