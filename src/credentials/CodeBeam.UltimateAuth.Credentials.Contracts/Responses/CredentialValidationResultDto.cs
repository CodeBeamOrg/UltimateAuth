namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialValidationResultDto
{
    public bool IsValid { get; init; }

    public bool RequiresReauthentication { get; init; }
    public bool RequiresSecurityVersionIncrement { get; init; }

    public string? FailureReason { get; init; }
}
