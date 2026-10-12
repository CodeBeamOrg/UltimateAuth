namespace CodeBeam.UltimateAuth.Core.Domain;

public enum RefreshOutcome
{
    None = 0,
    Success = 10,        // minimal transport
    NoOp = 20,
    Touched = 30,
    Rotated = 40,
    ReauthRequired = 100
}
