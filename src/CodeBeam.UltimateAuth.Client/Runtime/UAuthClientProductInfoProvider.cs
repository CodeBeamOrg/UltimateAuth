using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Client.Runtime;

internal sealed class UAuthClientProductInfoProvider : IUAuthClientProductInfoProvider
{
    private readonly UAuthClientProductInfo _info;

    public UAuthClientProductInfoProvider(IOptions<UAuthClientOptions> options)
    {
        var asm = typeof(UAuthClientProductInfoProvider).Assembly;
        var opts = options.Value;

        _info = new UAuthClientProductInfo
        {
            Version = asm.GetName().Version?.ToString(3) ?? "unknown",
            InformationalVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            StartedAt = DateTimeOffset.UtcNow,
            ClientProfile = opts.ClientProfile,

            AutoRefreshEnabled = opts.AutoRefresh.Enabled,
            RefreshInterval = opts.AutoRefresh.Interval,
            ReauthBehavior = opts.Reauth.Behavior,

            FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        };
    }

    public UAuthClientProductInfo Get() => _info;
}
