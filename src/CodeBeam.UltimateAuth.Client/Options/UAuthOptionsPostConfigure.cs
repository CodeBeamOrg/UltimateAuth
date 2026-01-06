using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal sealed class UAuthOptionsPostConfigure : IPostConfigureOptions<UAuthOptions>
    {
        private readonly IClientProfileDetector _detector;
        private readonly IServiceProvider _services;

        public UAuthOptionsPostConfigure(IClientProfileDetector detector, IServiceProvider services)
        {
            _detector = detector;
            _services = services;
        }

        public void PostConfigure(string? name, UAuthOptions options)
        {
            if (!options.AutoDetectClientProfile)
                return;

            if (options.ClientProfile != UAuthClientProfile.NotSpecified)
                return;

            options.ClientProfile = _detector.Detect(_services);
        }
    }
}
