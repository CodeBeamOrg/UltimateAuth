using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed class CredentialSecurityState
{
    public DateTimeOffset? RevokedAt { get; }
    public DateTimeOffset? LockedUntil { get; }
    public DateTimeOffset? ExpiresAt { get; }
    public DateTimeOffset? ResetRequestedAt { get; }
    public DateTimeOffset? ResetExpiresAt { get; }
    public DateTimeOffset? ResetConsumedAt { get; }
    public int FailedAttemptCount { get; }
    public DateTimeOffset? LastFailedAt { get; }
    public Guid SecurityStamp { get; }

    public CredentialSecurityState(
        DateTimeOffset? revokedAt = null,
        DateTimeOffset? lockedUntil = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? resetRequestedAt = null,
        DateTimeOffset? resetExpiresAt = null,
        DateTimeOffset? resetConsumedAt = null,
        int failedAttemptCount = 0,
        DateTimeOffset? lastFailedAt = null,
        Guid securityStamp = default)
    {
        RevokedAt = revokedAt;
        LockedUntil = lockedUntil;
        ExpiresAt = expiresAt;
        ResetRequestedAt = resetRequestedAt;
        ResetExpiresAt = resetExpiresAt;
        ResetConsumedAt = resetConsumedAt;
        FailedAttemptCount = failedAttemptCount;
        LastFailedAt = lastFailedAt;
        SecurityStamp = securityStamp;
    }

    public CredentialSecurityStatus Status(DateTimeOffset now)
    {
        if (RevokedAt is not null)
            return CredentialSecurityStatus.Revoked;

        if (LockedUntil is not null && LockedUntil > now)
            return CredentialSecurityStatus.Locked;

        if (ExpiresAt is not null && ExpiresAt <= now)
            return CredentialSecurityStatus.Expired;

        if (ResetRequestedAt is not null)
        {
            if (ResetConsumedAt is not null)
                return CredentialSecurityStatus.Active;

            if (ResetExpiresAt is not null && ResetExpiresAt <= now)
                return CredentialSecurityStatus.Active;

            return CredentialSecurityStatus.ResetRequested;
        }

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
            lockedUntil: null,
            expiresAt: null,
            resetRequestedAt: null,
            resetExpiresAt: null,
            resetConsumedAt: null,
            failedAttemptCount: 0,
            lastFailedAt: null,
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
            lockedUntil: LockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: ResetRequestedAt,
            resetExpiresAt: ResetExpiresAt,
            resetConsumedAt: ResetConsumedAt,
            failedAttemptCount: FailedAttemptCount,
            lastFailedAt: LastFailedAt,
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
            lockedUntil: LockedUntil,
            expiresAt: expiresAt,
            resetRequestedAt: ResetRequestedAt,
            resetExpiresAt: ResetExpiresAt,
            resetConsumedAt: ResetConsumedAt,
            failedAttemptCount: FailedAttemptCount,
            lastFailedAt: LastFailedAt,
            securityStamp: EnsureStamp(SecurityStamp)
        );
    }

    private static Guid EnsureStamp(Guid stamp) => stamp == Guid.Empty ? Guid.NewGuid() : stamp;

    public CredentialSecurityState RotateStamp()
    {
        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: LockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: ResetRequestedAt,
            resetExpiresAt: ResetExpiresAt,
            resetConsumedAt: ResetConsumedAt,
            failedAttemptCount: FailedAttemptCount,
            lastFailedAt: LastFailedAt,
            securityStamp: Guid.NewGuid()
        );
    }

    public CredentialSecurityState RegisterSuccessfulAuthentication()
    {
        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: null,
            expiresAt: ExpiresAt,
            resetRequestedAt: ResetRequestedAt,
            resetExpiresAt: ResetExpiresAt,
            resetConsumedAt: ResetConsumedAt,
            failedAttemptCount: 0,
            lastFailedAt: null,
            securityStamp: EnsureStamp(SecurityStamp)
        );
    }

    public CredentialSecurityState RegisterFailedAttempt(DateTimeOffset now, int threshold, TimeSpan lockoutDuration)
    {
        if (threshold <= 0)
            throw new UAuthValidationException(nameof(threshold));

        var failed = FailedAttemptCount + 1;

        var newLockedUntil = LockedUntil;

        if (failed >= threshold)
        {
            var candidate = now.Add(lockoutDuration);

            if (LockedUntil is null || candidate > LockedUntil)
                newLockedUntil = candidate;
        }

        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: newLockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: ResetRequestedAt,
            resetExpiresAt: ResetExpiresAt,
            resetConsumedAt: ResetConsumedAt,
            failedAttemptCount: failed,
            lastFailedAt: now,
            securityStamp: EnsureStamp(SecurityStamp)
        );
    }

    public CredentialSecurityState BeginReset(DateTimeOffset now, TimeSpan validity)
    {
        if (validity <= TimeSpan.Zero)
            throw new UAuthValidationException("credential_lockout_threshold_invalid");

        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: LockedUntil,
            expiresAt: ExpiresAt,
            resetRequestedAt: now,
            resetExpiresAt: now.Add(validity),
            resetConsumedAt: null,
            failedAttemptCount: FailedAttemptCount,
            lastFailedAt: LastFailedAt,
            securityStamp: Guid.NewGuid()
        );
    }

    public CredentialSecurityState CompleteReset(DateTimeOffset now, bool rotateStamp = true)
    {
        if (ResetRequestedAt is null)
            throw new UAuthValidationException("reset_not_requested");

        if (ResetConsumedAt is not null)
            throw new UAuthValidationException("reset_already_consumed");

        if (ResetExpiresAt is not null && ResetExpiresAt <= now)
            throw new UAuthValidationException("reset_expired");

        return new CredentialSecurityState(
            revokedAt: RevokedAt,
            lockedUntil: null,
            expiresAt: ExpiresAt,
            resetRequestedAt: null,
            resetExpiresAt: null,
            resetConsumedAt: now,
            failedAttemptCount: 0,
            lastFailedAt: null,
            securityStamp: rotateStamp ? Guid.NewGuid() : EnsureStamp(SecurityStamp)
        );
    }
}
