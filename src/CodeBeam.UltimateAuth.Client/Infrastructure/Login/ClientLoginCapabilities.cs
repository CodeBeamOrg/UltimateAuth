using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class ClientLoginCapabilities
{
    public static bool CanPostCredentials(UAuthClientProfile profile)
        => profile switch
        {
            UAuthClientProfile.BlazorServer => true,
            UAuthClientProfile.UAuthHub => true,
            _ => false
        };
}
