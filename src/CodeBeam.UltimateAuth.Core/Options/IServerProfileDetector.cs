using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Core.Options
{
    public interface IServerProfileDetector
    {
        UAuthClientProfile Detect(IServiceProvider services);
    }
}
