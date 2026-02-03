namespace CodeBeam.UltimateAuth.Server.Flows;

public enum PkceValidationFailureReason
{
    None,
    ArtifactExpired,
    MaxAttemptsExceeded,
    UnsupportedChallengeMethod,
    InvalidVerifier,
    ContextMismatch
}
