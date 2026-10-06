using CodeBeam.UltimateAuth.Client.Infrastructure;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class UAuthLoginPageResolver : IUAuthLoginPageResolver
{
    public string Resolve()
    {
        return UAuthLoginPageDiscovery.Resolve();
    }
}