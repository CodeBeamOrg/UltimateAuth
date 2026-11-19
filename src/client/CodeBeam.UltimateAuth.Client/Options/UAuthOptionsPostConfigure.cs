using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class UAuthClientOptionsPostConfigure : IPostConfigureOptions<UAuthClientOptions>
{
    private readonly IApplicationProfileDetector _detector;
    private readonly IServiceProvider _services;

    public UAuthClientOptionsPostConfigure(IApplicationProfileDetector detector, IServiceProvider services)
    {
        _detector = detector;
        _services = services;
    }

    public void PostConfigure(string? name, UAuthClientOptions options)
    {
        if (!options.AutoDetectClientProfile)
            return;

        if (options.ClientProfile != UAuthApplicationProfile.NotSpecified)
            return;

        options.ClientProfile = _detector.Detect(_services);
    }
}
