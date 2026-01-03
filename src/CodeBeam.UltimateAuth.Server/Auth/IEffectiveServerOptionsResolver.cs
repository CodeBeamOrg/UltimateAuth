using CodeBeam.UltimateAuth.Core;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IEffectiveServerOptionsResolver
    {
        UAuthMode? GetConfiguredMode();
        EffectiveUAuthServerOptions Resolve(UAuthMode mode);
    }
}
