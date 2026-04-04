namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthArtifactType
{
    PkceAuthorizationCode,
    HubFlow,
    LoginPreview,
    HubLogin,
    MfaChallenge,
    PasswordReset,
    MagicLink,
    OAuthState,
    Custom = 1000
}
