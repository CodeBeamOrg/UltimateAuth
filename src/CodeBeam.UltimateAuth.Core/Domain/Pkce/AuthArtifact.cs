namespace CodeBeam.UltimateAuth.Core.Domain;

public abstract class AuthArtifact
{
    protected AuthArtifact(AuthArtifactType type, DateTimeOffset expiresAt, int maxAttempts)
    {
        Type = type;
        ExpiresAt = expiresAt;
        MaxAttempts = maxAttempts;
    }

    public AuthArtifactType Type { get; }

    public DateTimeOffset ExpiresAt { get; internal set; }

    public int MaxAttempts { get; }

    public int AttemptCount { get; private set; }
    public bool IsCompleted { get; internal set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool CanAttempt() => AttemptCount < MaxAttempts;

    public void RegisterAttempt()
    {
        AttemptCount++;
    }
}
