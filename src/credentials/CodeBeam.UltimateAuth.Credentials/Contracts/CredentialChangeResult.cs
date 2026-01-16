namespace CodeBeam.UltimateAuth.Credentials;

public sealed record CredentialChangeResult(
    bool Succeeded,
    bool SecurityInvalidated,
    string? FailureReason = null);
