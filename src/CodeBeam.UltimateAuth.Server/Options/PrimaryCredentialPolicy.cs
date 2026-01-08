using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class PrimaryCredentialPolicy
    {
        /// <summary>
        /// Default primary credential for UI-style requests.
        /// </summary>
        public PrimaryCredentialKind Ui { get; set; } = PrimaryCredentialKind.Stateful;

        /// <summary>
        /// Default primary credential for API requests.
        /// </summary>
        public PrimaryCredentialKind Api { get; set; } = PrimaryCredentialKind.Stateless;

        internal PrimaryCredentialPolicy Clone() => new()
        {
            Ui = Ui,
            Api = Api
        };

    }
}
