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

        _info = new UAuthClientProductInfo
        {
            Version = asm.GetName().Version?.ToString(3) ?? "unknown",
            InformationalVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            StartedAt = DateTimeOffset.UtcNow,
            ClientProfile = options.Value.ClientProfile
        };
    }

    public UAuthClientProductInfo Get() => _info;
}
