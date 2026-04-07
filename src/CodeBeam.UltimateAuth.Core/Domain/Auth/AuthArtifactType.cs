namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthArtifactType
{
    PkceAuthorizationCode = 0,
    HubFlow = 10,
    LoginPreview = 20,
    HubLogin = 30,
    MfaChallenge = 40,
    PasswordReset = 50,
    MagicLink = 60,
    OAuthState = 100,
    Custom = 1000
}
