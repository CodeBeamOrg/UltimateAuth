using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class AuthFailureReasonExtensions
{
    public static string ToDefaultCode(this AuthFailureReason reason)
        => reason switch
        {
            AuthFailureReason.InvalidCredentials => "invalid_credentials",
            AuthFailureReason.LockedOut => "locked",
            AuthFailureReason.RequiresMfa => "mfa_required",
            AuthFailureReason.SessionExpired => "session_expired",
            AuthFailureReason.SessionRevoked => "session_revoked",
            AuthFailureReason.TenantDisabled => "tenant_disabled",
            AuthFailureReason.Unauthorized => "unauthorized",
            AuthFailureReason.ReauthenticationRequired => "reauthentication_required",
            _ => "failed"
        };
}
