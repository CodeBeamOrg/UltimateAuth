using CodeBeam.UltimateAuth.Core.Security;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

internal static class AuthenticationSecurityStateMapper
{
    public static AuthenticationSecurityState ToDomain(AuthenticationSecurityStateProjection p)
    {
        return AuthenticationSecurityState.FromProjection(
            p.Id,
            p.Tenant,
            p.UserKey,
            p.Scope,
            p.CredentialType,
            p.FailedAttempts,
            p.LastFailedAt,
            p.LockedUntil,
            p.RequiresReauthentication,
            p.ResetRequestedAt,
            p.ResetExpiresAt,
            p.ResetConsumedAt,
            p.ResetTokenHash,
            p.ResetAttempts,
            p.SecurityVersion);
    }

    public static AuthenticationSecurityStateProjection ToProjection(AuthenticationSecurityState d)
    {
        return new AuthenticationSecurityStateProjection
        {
            Id = d.Id,
            Tenant = d.Tenant,
            UserKey = d.UserKey,
            Scope = d.Scope,
            CredentialType = d.CredentialType,
            FailedAttempts = d.FailedAttempts,
            LastFailedAt = d.LastFailedAt,
            LockedUntil = d.LockedUntil,
            RequiresReauthentication = d.RequiresReauthentication,
            ResetRequestedAt = d.ResetRequestedAt,
            ResetExpiresAt = d.ResetExpiresAt,
            ResetConsumedAt = d.ResetConsumedAt,
            ResetTokenHash = d.ResetTokenHash,
            ResetAttempts = d.ResetAttempts,
            SecurityVersion = d.SecurityVersion
        };
    }

    public static void UpdateProjection(AuthenticationSecurityState d, AuthenticationSecurityStateProjection p)
    {
        p.FailedAttempts = d.FailedAttempts;
        p.LastFailedAt = d.LastFailedAt;
        p.LockedUntil = d.LockedUntil;
        p.RequiresReauthentication = d.RequiresReauthentication;

        p.ResetRequestedAt = d.ResetRequestedAt;
        p.ResetExpiresAt = d.ResetExpiresAt;
        p.ResetConsumedAt = d.ResetConsumedAt;
        p.ResetTokenHash = d.ResetTokenHash;
        p.ResetAttempts = d.ResetAttempts;

        p.SecurityVersion = d.SecurityVersion;
    }
}

