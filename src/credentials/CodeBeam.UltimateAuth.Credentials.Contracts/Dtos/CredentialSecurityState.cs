using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed class CredentialSecurityState
{
    public DateTimeOffset? RevokedAt { get; }
    public DateTimeOffset? ExpiresAt { get; }
    public Guid SecurityStamp { get; }

    public bool IsRevoked => RevokedAt != null;
    public bool IsExpired(DateTimeOffset now) => ExpiresAt != null && ExpiresAt <= now;

    public CredentialSecurityState(
        DateTimeOffset? revokedAt = null,
        DateTimeOffset? expiresAt = null,
        Guid securityStamp = default)
    {
        RevokedAt = revokedAt;
        ExpiresAt = expiresAt;
        SecurityStamp = securityStamp;
    }

    public CredentialSecurityStatus Status(DateTimeOffset now)
    {
        if (RevokedAt is not null)
            return CredentialSecurityStatus.Revoked;

        if (IsExpired(now))
            return CredentialSecurityStatus.Expired;

        return CredentialSecurityStatus.Active;
    }

    /// <summary>
    /// Determines whether the credential can be used at the given time.
    /// </summary>
    public bool IsUsable(DateTimeOffset now) => Status(now) == CredentialSecurityStatus.Active;

    public static CredentialSecurityState Active(Guid? securityStamp = null)
    {
        return new CredentialSecurityState(
            revokedAt: null,
            expiresAt: null,
            securityStamp: securityStamp ?? Guid.NewGuid()
        );
    }

    /// <summary>
    /// Revokes the credential permanently.
    /// </summary>
    public CredentialSecurityState Revoke(DateTimeOffset now)
    {
        if (RevokedAt is not null)
            return this;

        return new CredentialSecurityState(
            revokedAt: now,
            expiresAt: ExpiresAt,
            securityStamp: Guid.NewGuid()
        );
    }

    /// <summary>
    /// Sets or clears expiry while preserving the rest of the state.
    /// </summary>
    public CredentialSecurityState SetExpiry(DateTimeOffset? expiresAt)
    {
        // optional: normalize already-expired value? keep as-is; domain policy can decide.
        if (ExpiresAt == expiresAt)
            return this;

        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            expiresAt: expiresAt,
            securityStamp: EnsureStamp(SecurityStamp)
        );
    }

    private static Guid EnsureStamp(Guid stamp) => stamp == Guid.Empty ? Guid.NewGuid() : stamp;

    public CredentialSecurityState RotateStamp()
    {
        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            expiresAt: ExpiresAt,
            securityStamp: Guid.NewGuid()
        );
    }
}
