using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public sealed record EffectiveAuthResponse(
        CredentialResponseOptions SessionId,
        CredentialResponseOptions AccessToken,
        CredentialResponseOptions RefreshToken,
        bool LoginRedirectEnabled,
        bool LogoutRedirectEnabled
    );
}
