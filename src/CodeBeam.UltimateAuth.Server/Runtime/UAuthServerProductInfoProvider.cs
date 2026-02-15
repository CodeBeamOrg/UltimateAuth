using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Server.Runtime;

internal sealed class UAuthServerProductInfoProvider : IUAuthServerProductInfoProvider
{
    private readonly UAuthServerProductInfo _info;

    public UAuthServerProductInfoProvider(IOptions<UAuthServerOptions> serverOptions)
    {
        var asm = typeof(UAuthServerProductInfoProvider).Assembly;

        _info = new UAuthServerProductInfo
        {
            Version = asm.GetName().Version?.ToString(3) ?? "unknown",
            InformationalVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            StartedAt = DateTimeOffset.UtcNow,

            HubDeploymentMode = serverOptions.Value.HubDeploymentMode,
            MultiTenancyEnabled = serverOptions.Value.MultiTenant.Enabled,
            FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        };
    }

    public UAuthServerProductInfo Get() => _info;
}
