using CodeBeam.UltimateAuth.Core.Runtime;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class UAuthOptionsPostConfigureGuard : IPostConfigureOptions<UAuthOptions>
{
    private readonly DirectCoreConfigurationMarker _directConfigMarker;
    private readonly IEnumerable<IUAuthRuntimeMarker> _runtimeMarkers;

    public UAuthOptionsPostConfigureGuard(DirectCoreConfigurationMarker directConfigMarker, IEnumerable<IUAuthRuntimeMarker> runtimeMarkers)
    {
        _directConfigMarker = directConfigMarker;
        _runtimeMarkers = runtimeMarkers;
    }

    public void PostConfigure(string? name, UAuthOptions options)
    {
        var hasServerRuntime = _runtimeMarkers.Any();

        if (!_directConfigMarker.IsConfigured)
        {
            return;
        }

        if (hasServerRuntime)
        {
            throw new InvalidOperationException("Direct core configuration is not allowed in server-hosted applications. " +
                "Configure authentication policies via AddUltimateAuthServer instead.");
        }

        if (!options.AllowDirectCoreConfiguration)
        {
            throw new InvalidOperationException("Direct core configuration is not allowed. " +
                "Set AllowDirectCoreConfiguration = true only for advanced, non-server scenarios.");
        }
    }
}
