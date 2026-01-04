using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Contracts;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class CredentialResponseOptions
    {
        public TokenResponseMode Mode { get; set; } = TokenResponseMode.None;

        /// <summary>
        /// Header or body name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Applies when Mode = Header
        /// </summary>
        public HeaderTokenFormat HeaderFormat { get; set; } = HeaderTokenFormat.Bearer;
        public TokenFormat TokenFormat { get; set; }

        // Only for cookie
        public UAuthCookieOptions? Cookie { get; init; }

        internal CredentialResponseOptions Clone() => new()
        {
            Mode = Mode,
            Name = Name,
            HeaderFormat = HeaderFormat,
            TokenFormat = TokenFormat,
            Cookie = Cookie?.Clone()
        };

    }
}
