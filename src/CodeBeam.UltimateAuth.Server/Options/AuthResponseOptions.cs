using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class AuthResponseOptions
    {
        public CredentialResponseOptions SessionIdDelivery { get; set; } = new();
        public CredentialResponseOptions AccessTokenDelivery { get; set; } = new();
        public CredentialResponseOptions RefreshTokenDelivery { get; set; } = new();

        public RedirectResponseOptions Redirect { get; set; } = new();
    }
}
