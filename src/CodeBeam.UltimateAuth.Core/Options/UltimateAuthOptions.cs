using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Events;

namespace CodeBeam.UltimateAuth.Core.Options
{
    public sealed class UltimateAuthOptions
    {
        public LoginOptions Login { get; set; } = new();
        public SessionOptions Session { get; set; } = new();
        public TokenOptions Token { get; set; } = new();
        public PkceOptions Pkce { get; set; } = new();
        public UAuthEvents UAuthEvents { get; set; } = new();
        public MultiTenantOptions MultiTenantOptions { get; set; }

        /// <summary>
        /// Provides TUserId converters for user id normalization.
        /// </summary>
        public IUserIdConverterResolver? UserIdConverters { get; set; }
    }
}
