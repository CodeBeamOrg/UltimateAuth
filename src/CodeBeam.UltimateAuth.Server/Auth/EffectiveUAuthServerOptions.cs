using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public sealed class EffectiveUAuthServerOptions
    {
        public UAuthMode Mode { get; init; }
        public UAuthServerOptions Options { get; init; } = default!;
    }
}
