using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Core.Runtime;

internal sealed class UAuthProductInfoProvider : IUAuthProductInfoProvider
{
    private readonly UAuthProductInfo _info;

    public UAuthProductInfoProvider(IOptions<UAuthOptions> options)
    {
        var asm = typeof(UAuthProductInfoProvider).Assembly;

        _info = new UAuthProductInfo
        {
            Version = asm.GetName().Version?.ToString(3) ?? "unknown",
            InformationalVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    public UAuthProductInfo Get() => _info;
}
