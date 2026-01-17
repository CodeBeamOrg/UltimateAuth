using CodeBeam.UltimateAuth.Security.Argon2;

namespace CodeBeam.UltimateAuth.Server.Composition.Extensions;

public static class UltimateAuthServerBuilderArgon2Extensions
{
    public static UltimateAuthServerBuilder UseArgon2(this UltimateAuthServerBuilder builder, Action<Argon2Options>? configure = null)
    {
        builder.Services.AddUltimateAuthArgon2(configure);
        return builder;
    }
}
