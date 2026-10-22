using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public enum SandboxAccess
{
    Denied = 0,
    ReadOnly = 1,
    Full = 2
}

public static class SandboxAccessPolicy
{
    public static SandboxAccess Evaluate(SandboxStatus status)
        => status switch
        {
            SandboxStatus.Active => SandboxAccess.Full,
            SandboxStatus.Passive => SandboxAccess.ReadOnly,
            SandboxStatus.Suspended => SandboxAccess.Denied,
            _ => SandboxAccess.Denied
        };
}
