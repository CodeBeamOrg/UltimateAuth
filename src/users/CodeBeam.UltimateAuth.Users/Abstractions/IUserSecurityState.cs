namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityState
{
    long SecurityVersion { get; }
    int FailedLoginAttempts { get; }
    DateTimeOffset? LockedUntil { get; }
    bool RequiresReauthentication { get; }
    DateTimeOffset? LastFailedAt { get; }

    bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTimeOffset.UtcNow;
}
