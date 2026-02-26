namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed class CredentialSecurityState
{
    public DateTimeOffset? RevokedAt { get; }
    public DateTimeOffset? LockedUntil { get; }
    public DateTimeOffset? ExpiresAt { get; }
    public DateTimeOffset? ResetRequestedAt { get; init; }
    public Guid SecurityStamp { get; }

    public CredentialSecurityStatus Status(DateTimeOffset now)
    {
        if (RevokedAt is not null)
            return CredentialSecurityStatus.Revoked;

        if (LockedUntil is not null && LockedUntil > now)
            return CredentialSecurityStatus.Locked;

        if (ExpiresAt is not null && ExpiresAt <= now)
            return CredentialSecurityStatus.Expired;

        if (ResetRequestedAt is not null)
            return CredentialSecurityStatus.ResetRequested;

        return CredentialSecurityStatus.Active;
    }

    public CredentialSecurityState(
        DateTimeOffset? revokedAt = null,
        DateTimeOffset? lockedUntil = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? resetRequestedAt = null,
        Guid securityStamp = default)
    {
        RevokedAt = revokedAt;
        LockedUntil = lockedUntil;
        ExpiresAt = expiresAt;
        ResetRequestedAt = resetRequestedAt;
        SecurityStamp = securityStamp;
    }

    /// <summary>
    /// Determines whether the credential can be used at the given time.
    /// </summary>
    public bool IsUsable(DateTimeOffset now) => Status(now) == CredentialSecurityStatus.Active;

    public static CredentialSecurityState Active(Guid? securityStamp = null)
    {
        return new CredentialSecurityState(
            revokedAt: null,
            lockedUntil: null,
            expiresAt: null,
            resetRequestedAt: null,
            securityStamp: securityStamp ?? Guid.NewGuid());
    }

    public CredentialSecurityState Revoke(DateTimeOffset now)
    {
        return new CredentialSecurityState(
            revokedAt: now,
            lockedUntil: LockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: ResetRequestedAt,
            securityStamp: Guid.NewGuid());
    }

    public CredentialSecurityState SetExpiry(DateTimeOffset? expiresAt)
    {
        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: LockedUntil,
            expiresAt: expiresAt,
            resetRequestedAt: ResetRequestedAt,
            securityStamp: SecurityStamp);
    }

    public CredentialSecurityState BeginReset(DateTimeOffset now, bool rotateStamp = true)
    => new(
        revokedAt: RevokedAt,
        lockedUntil: LockedUntil,
        expiresAt: ExpiresAt,
        resetRequestedAt: now,
        securityStamp: rotateStamp ? Guid.NewGuid() : SecurityStamp
    );

    public CredentialSecurityState CompleteReset(bool rotateStamp = true)
        => new(
            revokedAt: RevokedAt,
            lockedUntil: LockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: null,
            securityStamp: rotateStamp ? Guid.NewGuid() : SecurityStamp
        );

    public CredentialSecurityState RotateStamp()
    {
        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: LockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: ResetRequestedAt,
            securityStamp: Guid.NewGuid());
    }
}
