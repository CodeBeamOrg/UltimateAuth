namespace CodeBeam.UltimateAuth.Core.Domain;

public enum SessionRefreshStatus
{
    Success,
    ReauthRequired,
    InvalidRequest,
    Failed
}
