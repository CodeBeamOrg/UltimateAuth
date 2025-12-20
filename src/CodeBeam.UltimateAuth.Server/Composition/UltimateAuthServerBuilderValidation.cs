using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Composition;

public static class UltimateAuthServerBuilderValidationExtensions
{
    public static IServiceCollection Build(this UltimateAuthServerBuilder builder)
    {
        var services = builder.Services;

        if (!services.Any(sd => sd.ServiceType == typeof(IUAuthPasswordHasher)))
            throw new InvalidOperationException("No IUAuthPasswordHasher registered. Call UseArgon2() or another hasher.");

        if (!services.Any(sd => sd.ServiceType.IsAssignableTo(typeof(IUAuthUserStore<>))))
            throw new InvalidOperationException("No credential store registered.");

        if (!services.Any(sd => sd.ServiceType.IsAssignableTo(typeof(ISessionStore<>))))
            throw new InvalidOperationException("No session store registered.");

        return services;
    }
}
