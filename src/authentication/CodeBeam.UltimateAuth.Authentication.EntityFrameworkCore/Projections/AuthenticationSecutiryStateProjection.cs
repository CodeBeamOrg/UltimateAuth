using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

public sealed class AuthenticationSecurityStateProjection
{
    public Guid Id { get; set; }

    public TenantKey Tenant { get; set; }
    public UserKey UserKey { get; set; }

    public AuthenticationSecurityScope Scope { get; set; }
    public CredentialType? CredentialType { get; set; }

    public int FailedAttempts { get; set; }
    public DateTimeOffset? LastFailedAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public bool RequiresReauthentication { get; set; }

    public DateTimeOffset? ResetRequestedAt { get; set; }
    public DateTimeOffset? ResetExpiresAt { get; set; }
    public DateTimeOffset? ResetConsumedAt { get; set; }
    public string? ResetTokenHash { get; set; }
    public int ResetAttempts { get; set; }

    public long SecurityVersion { get; set; }
}
