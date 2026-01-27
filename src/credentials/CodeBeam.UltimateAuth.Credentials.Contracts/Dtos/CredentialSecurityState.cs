using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed class CredentialSecurityState
{
    public CredentialSecurityStatus Status { get; }
    public DateTimeOffset? RestrictedUntil { get; }
    public DateTimeOffset? ExpiresAt { get; }
    public string? Reason { get; }

    public CredentialSecurityState(
        CredentialSecurityStatus status,
        DateTimeOffset? restrictedUntil = null,
        DateTimeOffset? expiresAt = null,
        string? reason = null)
    {
        Status = status;
        RestrictedUntil = restrictedUntil;
        ExpiresAt = expiresAt;
        Reason = reason;
    }

    /// <summary>
    /// Determines whether the credential can be used at the given time.
    /// </summary>
    public bool IsUsable(DateTimeOffset now)
    {
        if (Status == CredentialSecurityStatus.Expired)
            return false;

        if (ExpiresAt is not null && ExpiresAt <= now)
            return false;

        if ((Status == CredentialSecurityStatus.Locked || Status == CredentialSecurityStatus.Revoked) && RestrictedUntil is not null)
        {
            return RestrictedUntil <= now;
        }

        if (Status == CredentialSecurityStatus.Locked || Status == CredentialSecurityStatus.Revoked)
            return false;

        return true;
    }
}
