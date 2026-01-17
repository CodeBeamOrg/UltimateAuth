namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialChangeResult(
    bool Succeeded,
    bool SecurityInvalidated,
    string? FailureReason = null);
