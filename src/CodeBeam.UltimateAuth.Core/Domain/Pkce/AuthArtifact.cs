namespace CodeBeam.UltimateAuth.Core.Domain;

public abstract class AuthArtifact
{
    protected AuthArtifact(AuthArtifactType type, DateTimeOffset expiresAt)
    {
        Type = type;
        ExpiresAt = expiresAt;
    }

    public AuthArtifactType Type { get; }

    public DateTimeOffset ExpiresAt { get; internal set; }

    public int AttemptCount { get; private set; }
    public bool IsCompleted { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void RegisterAttempt()
    {
        AttemptCount++;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
