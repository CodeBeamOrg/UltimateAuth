using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class AuthResponseOptions
    {
        public CredentialResponseOptions SessionIdDelivery { get; set; } = new();
        public CredentialResponseOptions AccessTokenDelivery { get; set; } = new();
        public CredentialResponseOptions RefreshTokenDelivery { get; set; } = new();

        public LoginRedirectOptions Login { get; set; } = new();
        public LogoutRedirectOptions Logout { get; set; } = new();
    }
}
