using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record LockoutQueryRequest(
    AuthenticationSecurityScope Scope,
    CredentialType? CredentialType = null);

public sealed record UnlockAuthenticationRequest(
    AuthenticationSecurityScope Scope,
    CredentialType? CredentialType = null);

public sealed record AuthenticationLockoutInfo(
    AuthenticationSecurityScope Scope,
    CredentialType? CredentialType,
    bool IsLocked,
    int FailedAttempts,
    DateTimeOffset? LastFailedAt,
    DateTimeOffset? LockedUntil,
    bool RequiresReauthentication,
    long SecurityVersion);

public sealed record AuthenticationUnlockResult(
    AuthenticationSecurityScope Scope,
    CredentialType? CredentialType,
    AuthenticationLockoutInfo State);
