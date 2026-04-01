namespace CodeBeam.UltimateAuth.Core.Domain;

public enum SessionRefreshStatus
{
    Success = 0,
    ReauthRequired = 10,
    InvalidRequest = 20,
    Failed = 30
}
