using CodeBeam.UltimateAuth.Core.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class CoreConfigurationIntentDetector : IPostConfigureOptions<UAuthOptions>
{
    private readonly DirectCoreConfigurationMarker _marker;
    private readonly IConfiguration? _configuration;

    public CoreConfigurationIntentDetector(DirectCoreConfigurationMarker marker, IConfiguration? configuration)
    {
        _marker = marker;
        _configuration = configuration;
    }

    public void PostConfigure(string? name, UAuthOptions options)
    {
        if (_configuration is null)
            return;

        var coreSection = _configuration.GetSection("UltimateAuth:Core");
        if (coreSection.Exists())
        {
            _marker.MarkConfigured();
        }
    }
}
