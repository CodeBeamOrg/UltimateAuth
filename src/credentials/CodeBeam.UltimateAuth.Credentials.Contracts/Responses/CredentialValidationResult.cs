namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialValidationResult
{
    public bool IsValid { get; init; }
    public bool RequiresReauthentication { get; init; }
    public bool RequiresSecurityVersionIncrement { get; init; }
    public string? FailureReason { get; init; }

    public static CredentialValidationResult Success(
        bool requiresSecurityVersionIncrement = false)
        => new()
        {
            IsValid = true,
            RequiresSecurityVersionIncrement = requiresSecurityVersionIncrement
        };

    public static CredentialValidationResult Failed(
        string? reason = null,
        bool requiresReauthentication = false)
        => new()
        {
            IsValid = false,
            RequiresReauthentication = requiresReauthentication,
            FailureReason = reason
        };

    public static CredentialValidationResult ReauthenticationRequired(
        string? reason = null)
        => new()
        {
            IsValid = false,
            RequiresReauthentication = true,
            FailureReason = reason
        };
}
