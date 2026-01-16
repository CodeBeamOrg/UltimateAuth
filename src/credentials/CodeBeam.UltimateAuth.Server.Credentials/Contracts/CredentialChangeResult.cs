namespace CodeBeam.UltimateAuth.Server.Credentials;

public sealed record CredentialChangeResult(
    bool Succeeded,
    bool SecurityInvalidated,
    string? FailureReason = null);
