namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthArtifactType
{
    PkceAuthorizationCode,
    HubLogin,
    MfaChallenge,
    PasswordReset,
    MagicLink,
    OAuthState,
    Custom = 1000
}
