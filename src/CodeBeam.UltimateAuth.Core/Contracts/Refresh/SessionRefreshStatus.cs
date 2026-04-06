namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum SessionRefreshStatus
{
    Success = 0,
    ReauthRequired = 10,
    InvalidRequest = 20,
    Failed = 30
}
