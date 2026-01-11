namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public enum PkceValidationFailureReason
{
    None,
    ArtifactExpired,
    MaxAttemptsExceeded,
    UnsupportedChallengeMethod,
    InvalidVerifier,
    ContextMismatch
}
