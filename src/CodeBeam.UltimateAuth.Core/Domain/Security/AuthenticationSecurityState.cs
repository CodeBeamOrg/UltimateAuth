using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Security;

// TODO: Do not store reset token hash in db.
public sealed class AuthenticationSecurityState : ITenantEntity, IVersionedEntity, IEntitySnapshot<AuthenticationSecurityState>
{
    public Guid Id { get; }
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }
    public AuthenticationSecurityScope Scope { get; }
    public CredentialType? CredentialType { get; }

    public int FailedAttempts { get; }
    public DateTimeOffset? LastFailedAt { get; }
    public DateTimeOffset? LockedUntil { get; }
    public bool RequiresReauthentication { get; }

    public DateTimeOffset? ResetRequestedAt { get; }
    public DateTimeOffset? ResetExpiresAt { get; }
    public DateTimeOffset? ResetConsumedAt { get; }
    public string? ResetTokenHash { get; }
    public int ResetAttempts { get; }

    public long SecurityVersion { get; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil.HasValue && LockedUntil > now;
    public bool HasResetRequest => ResetRequestedAt is not null;


    long IVersionedEntity.Version
    {
        get => SecurityVersion;
        set => throw new NotSupportedException("AuthenticationSecurityState uses SecurityVersion.");
    }

    private AuthenticationSecurityState(
        Guid id,
        TenantKey tenant,
        UserKey userKey,
        AuthenticationSecurityScope scope,
        CredentialType? credentialType,
        int failedAttempts,
        DateTimeOffset? lastFailedAt,
        DateTimeOffset? lockedUntil,
        bool requiresReauthentication,
        DateTimeOffset? resetRequestedAt,
        DateTimeOffset? resetExpiresAt,
        DateTimeOffset? resetConsumedAt,
        string? resetTokenHash,
        int resetAttempts,
        long securityVersion)
    {
        if (id == Guid.Empty)
            throw new UAuthValidationException("security_state_id_required");

        if (scope == AuthenticationSecurityScope.Account && credentialType is not null)
            throw new UAuthValidationException("account_scope_must_not_have_credential_type");

        if (scope == AuthenticationSecurityScope.Factor && credentialType is null)
            throw new UAuthValidationException("factor_scope_requires_credential_type");

        Id = id;
        Tenant = tenant;
        UserKey = userKey;
        Scope = scope;
        CredentialType = credentialType;
        FailedAttempts = failedAttempts < 0 ? 0 : failedAttempts;
        LastFailedAt = lastFailedAt;
        LockedUntil = lockedUntil;
        RequiresReauthentication = requiresReauthentication;
        ResetRequestedAt = resetRequestedAt;
        ResetExpiresAt = resetExpiresAt;
        ResetConsumedAt = resetConsumedAt;
        ResetTokenHash = resetTokenHash;
        ResetAttempts = resetAttempts;
        SecurityVersion = securityVersion < 0 ? 0 : securityVersion;
    }

    public static AuthenticationSecurityState CreateAccount(TenantKey tenant, UserKey userKey, Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            tenant,
            userKey,
            AuthenticationSecurityScope.Account,
            credentialType: null,
            failedAttempts: 0,
            lastFailedAt: null,
            lockedUntil: null,
            requiresReauthentication: false,
            resetRequestedAt: null,
            resetExpiresAt: null,
            resetConsumedAt: null,
            resetTokenHash: null,
            resetAttempts: 0,
            securityVersion: 0);

    public static AuthenticationSecurityState CreateFactor(TenantKey tenant, UserKey userKey, CredentialType type, Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            tenant,
            userKey,
            AuthenticationSecurityScope.Factor,
            credentialType: type,
            failedAttempts: 0,
            lastFailedAt: null,
            lockedUntil: null,
            requiresReauthentication: false,
            resetRequestedAt: null,
            resetExpiresAt: null,
            resetConsumedAt: null,
            resetTokenHash: null,
            resetAttempts: 0,
            securityVersion: 0);

    /// <summary>
    /// Resets failures if the last failure is outside the given window.
    /// Keeps lock and reauth flags untouched by default.
    /// </summary>
    public AuthenticationSecurityState ResetFailuresIfWindowExpired(DateTimeOffset now, TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
            return this;

        if (LastFailedAt is not DateTimeOffset last)
            return this;

        if (now - last <= window)
            return this;

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            failedAttempts: 0,
            lastFailedAt: null,
            lockedUntil: LockedUntil,
            requiresReauthentication: RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);
    }

    /// <summary>
    /// Registers a failed authentication attempt. Optionally locks until now + duration when threshold reached.
    /// If already locked, may extend lock depending on extendLock.
    /// </summary>
    public AuthenticationSecurityState RegisterFailure(DateTimeOffset now, int threshold, TimeSpan lockoutDuration, bool extendLock = true)
    {
        if (threshold < 0)
            throw new UAuthValidationException(nameof(threshold));

        var nextCount = FailedAttempts + 1;

        DateTimeOffset? nextLockedUntil = LockedUntil;

        if (threshold > 0 && nextCount >= threshold)
        {
            var candidate = now.Add(lockoutDuration);

            if (nextLockedUntil is null)
                nextLockedUntil = candidate;
            else if (extendLock && candidate > nextLockedUntil)
                nextLockedUntil = candidate;
        }

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            failedAttempts: nextCount,
            lastFailedAt: now,
            lockedUntil: nextLockedUntil,
            RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);
    }

    /// <summary>
    /// Registers a successful authentication: clears failures and lock.
    /// </summary>
    public AuthenticationSecurityState RegisterSuccess()
        => new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            failedAttempts: 0,
            lastFailedAt: null,
            lockedUntil: null,
            RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);

    /// <summary>
    /// Admin/system unlock: clears lock and failures.
    /// </summary>
    public AuthenticationSecurityState Unlock()
        => new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            failedAttempts: 0,
            lastFailedAt: null,
            lockedUntil: null,
            RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);

    public AuthenticationSecurityState LockUntil(DateTimeOffset until, bool overwriteIfShorter = false)
    {
        DateTimeOffset? next = LockedUntil;

        if (next is null)
            next = until;
        else if (overwriteIfShorter || until > next)
            next = until;

        if (next == LockedUntil)
            return this;

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            lockedUntil: next,
            RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            SecurityVersion + 1);
    }

    public AuthenticationSecurityState RequireReauthentication()
    {
        if (RequiresReauthentication)
            return this;

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            LockedUntil,
            requiresReauthentication: true,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);
    }

    public AuthenticationSecurityState ClearReauthentication()
    {
        if (!RequiresReauthentication)
            return this;

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            LockedUntil,
            requiresReauthentication: false,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);
    }

    public bool HasActiveReset(DateTimeOffset now)
    {
        if (ResetRequestedAt is null)
            return false;

        if (ResetConsumedAt is not null)
            return false;

        if (ResetExpiresAt is not null && ResetExpiresAt <= now)
            return false;

        return true;
    }

    public bool IsResetExpired(DateTimeOffset now)
    {
        return ResetExpiresAt is not null && ResetExpiresAt <= now;
    }

    public AuthenticationSecurityState BeginReset(string tokenHash, DateTimeOffset now, TimeSpan validity)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new UAuthValidationException("reset_token_required");

        if (HasActiveReset(now))
            throw new UAuthConflictException("reset_already_active");

        var expires = now.Add(validity);

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            LockedUntil,
            RequiresReauthentication,
            resetRequestedAt: now,
            resetExpiresAt: expires,
            resetConsumedAt: null,
            resetTokenHash: tokenHash,
            resetAttempts: 0,
            securityVersion: SecurityVersion + 1);
    }

    public AuthenticationSecurityState RegisterResetFailure(DateTimeOffset now, int maxAttempts)
    {
        if (IsResetExpired(now))
            return ClearReset();

        var next = ResetAttempts + 1;

        if (next >= maxAttempts)
        {
            return ClearReset();
        }

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            LockedUntil,
            RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            ResetConsumedAt,
            ResetTokenHash,
            next,
            securityVersion: SecurityVersion + 1);
    }

    public AuthenticationSecurityState ConsumeReset(DateTimeOffset now)
    {
        if (ResetRequestedAt is null)
            throw new UAuthConflictException("reset_not_requested");

        if (ResetConsumedAt is not null)
            throw new UAuthConflictException("reset_already_used");

        if (IsResetExpired(now))
            throw new UAuthConflictException("reset_expired");

        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            LockedUntil,
            RequiresReauthentication,
            ResetRequestedAt,
            ResetExpiresAt,
            now,
            null,
            ResetAttempts,
            securityVersion: SecurityVersion + 1);
    }

    public AuthenticationSecurityState ClearReset()
    {
        return new AuthenticationSecurityState(
            Id,
            Tenant,
            UserKey,
            Scope,
            CredentialType,
            FailedAttempts,
            LastFailedAt,
            LockedUntil,
            RequiresReauthentication,
            null,
            null,
            null,
            null,
            0,
            securityVersion: SecurityVersion + 1);
    }

    public AuthenticationSecurityState Snapshot()
    {
        return new AuthenticationSecurityState(
            id: Id,
            tenant: Tenant,
            userKey: UserKey,
            scope: Scope,
            credentialType: CredentialType,
            failedAttempts: FailedAttempts,
            lastFailedAt: LastFailedAt,
            lockedUntil: LockedUntil,
            requiresReauthentication: RequiresReauthentication,
            resetRequestedAt: ResetRequestedAt,
            resetExpiresAt: ResetExpiresAt,
            resetConsumedAt: ResetConsumedAt,
            resetTokenHash: ResetTokenHash,
            resetAttempts: ResetAttempts,
            securityVersion: SecurityVersion
        );
    }

    public static AuthenticationSecurityState FromProjection(
        Guid id,
        TenantKey tenant,
        UserKey userKey,
        AuthenticationSecurityScope scope,
        CredentialType? credentialType,
        int failedAttempts,
        DateTimeOffset? lastFailedAt,
        DateTimeOffset? lockedUntil,
        bool requiresReauthentication,
        DateTimeOffset? resetRequestedAt,
        DateTimeOffset? resetExpiresAt,
        DateTimeOffset? resetConsumedAt,
        string? resetTokenHash,
        int resetAttempts,
        long securityVersion)
    {
        return new AuthenticationSecurityState(
            id,
            tenant,
            userKey,
            scope,
            credentialType,
            failedAttempts,
            lastFailedAt,
            lockedUntil,
            requiresReauthentication,
            resetRequestedAt,
            resetExpiresAt,
            resetConsumedAt,
            resetTokenHash,
            resetAttempts,
            securityVersion);
    }
}
