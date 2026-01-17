namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialProvisionResult
{
    public required bool Succeeded { get; init; }

    public CredentialType? Type { get; init; }

    /// <summary>
    /// Indicates whether existing security state was affected.
    /// For initial provisioning this is usually false.
    /// </summary>
    public bool SecurityInvalidated { get; init; }

    public string? FailureReason { get; init; }

    /* ----------------- Helpers ----------------- */

    public static CredentialProvisionResult Success(CredentialType type)
        => new()
        {
            Succeeded = true,
            Type = type,
            SecurityInvalidated = false
        };

    public static CredentialProvisionResult Failed(string reason)
        => new()
        {
            Succeeded = false,
            FailureReason = reason
        };
}
