using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Server.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Composition.Extensions;

public static class UltimateAuthServerBuilderArgon2Extensions
{
    public static UltimateAuthServerBuilder UseArgon2(
        this UltimateAuthServerBuilder builder,
        Action<Argon2Options>? configure = null)
    {
        builder.Services.AddUltimateAuthArgon2(configure);
        return builder;
    }
}
