using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class ClientLoginCapabilities
{
    public static bool CanPostCredentials(UAuthApplicationProfile profile)
        => profile switch
        {
            UAuthApplicationProfile.BlazorServer => true,
            UAuthApplicationProfile.UAuthHub => true,
            _ => false
        };
}
