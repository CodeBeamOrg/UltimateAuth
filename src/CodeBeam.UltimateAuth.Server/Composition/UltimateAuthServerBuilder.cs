using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Composition;

public sealed class UltimateAuthServerBuilder
{
    internal UltimateAuthServerBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}
