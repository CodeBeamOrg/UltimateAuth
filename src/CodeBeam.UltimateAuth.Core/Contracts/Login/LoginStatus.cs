namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum LoginStatus
{
    None = 0,
    Success = 10,
    RequiresContinuation = 20,
    Failed = 30
}
