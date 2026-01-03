namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class AuthResponseOptions
    {
        public CredentialResponseOptions SessionIdDelivery { get; set; } = new();
        public CredentialResponseOptions AccessTokenDelivery { get; set; } = new();
        public CredentialResponseOptions RefreshTokenDelivery { get; set; } = new();

        public LoginRedirectOptions Login { get; set; } = new();
        public LogoutRedirectOptions Logout { get; set; } = new();

        internal AuthResponseOptions Clone() => new()
        {
            SessionIdDelivery = SessionIdDelivery.Clone(),
            AccessTokenDelivery = AccessTokenDelivery.Clone(),
            RefreshTokenDelivery = RefreshTokenDelivery.Clone(),
            Login = Login.Clone(),
            Logout = Logout.Clone()
        };

    }
}
