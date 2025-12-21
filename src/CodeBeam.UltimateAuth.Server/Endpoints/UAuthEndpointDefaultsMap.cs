using CodeBeam.UltimateAuth.Core;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    /// <summary>
    /// Provides default endpoint enablement rules based on UAuthMode.
    /// These defaults represent the secure and meaningful surface
    /// for each authentication strategy.
    /// </summary>
    internal static class UAuthEndpointDefaultsMap
    {
        public static UAuthEndpointDefaults ForMode(UAuthMode? mode)
        {
            if (!mode.HasValue)
            {
                throw new InvalidOperationException(
                    "UAuthMode must be resolved before endpoint mapping. " +
                    "Ensure ClientProfile defaults are applied.");
            }

            return mode switch
            {
                UAuthMode.PureOpaque => new UAuthEndpointDefaults
                {
                    Login = true,
                    Pkce = false,
                    Token = false,
                    Session = true,
                    UserInfo = true
                },

                UAuthMode.Hybrid => new UAuthEndpointDefaults
                {
                    Login = true,
                    Pkce = true,
                    Token = true,
                    Session = true,
                    UserInfo = true
                },

                UAuthMode.SemiHybrid => new UAuthEndpointDefaults
                {
                    Login = true,
                    Pkce = true,
                    Token = true,
                    Session = false,
                    UserInfo = true
                },

                UAuthMode.PureJwt => new UAuthEndpointDefaults
                {
                    Login = true,
                    Pkce = false,
                    Token = true,
                    Session = false,
                    UserInfo = true
                },

                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
}
