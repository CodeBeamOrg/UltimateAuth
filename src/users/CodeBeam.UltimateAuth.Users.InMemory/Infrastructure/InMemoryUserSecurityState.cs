namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityState : IUserSecurityState
{
    public long SecurityVersion { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTimeOffset? LockedUntil { get; init; }
    public bool RequiresReauthentication { get; init; }
    public DateTimeOffset? LastFailedAt { get; init; }

    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTimeOffset.UtcNow;
}
