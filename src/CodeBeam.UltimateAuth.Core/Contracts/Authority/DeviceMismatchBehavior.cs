namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum DeviceMismatchBehavior
{
    Reject = 0,
    Allow = 10,
    AllowAndRebind = 20
}
