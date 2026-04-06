namespace CodeBeam.UltimateAuth.Server.Flows;

public enum PkceValidationFailureReason
{
    None = 0,
    ArtifactExpired = 10,
    MaxAttemptsExceeded = 20,
    UnsupportedChallengeMethod = 30,
    InvalidVerifier = 40,
    ContextMismatch = 50
}
