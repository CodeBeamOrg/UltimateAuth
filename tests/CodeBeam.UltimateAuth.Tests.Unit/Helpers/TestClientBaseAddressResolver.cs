using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestClientBaseAddressResolver
{
    public static ClientBaseAddressResolver Create()
    {
        var providers = new IClientBaseAddressProvider[]
        {
            new OriginHeaderBaseAddressProvider(),
            new RefererHeaderBaseAddressProvider(),
            new ConfiguredClientBaseAddressProvider(),
            new RequestHostBaseAddressProvider(),
        };

        return new ClientBaseAddressResolver(providers);
    }
}
