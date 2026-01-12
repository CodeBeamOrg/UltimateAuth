namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthArtifactType
{
    PkceAuthorizationCode,
    HubFlow,
    HubLogin,
    MfaChallenge,
    PasswordReset,
    MagicLink,
    OAuthState,
    Custom = 1000
}
