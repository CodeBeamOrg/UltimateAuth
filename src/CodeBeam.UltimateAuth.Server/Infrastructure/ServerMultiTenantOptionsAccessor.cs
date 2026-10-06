using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class ServerMultiTenantOptionsAccessor : IUAuthMultiTenantOptionsAccessor
{
    private readonly IOptions<UAuthServerOptions> _options;

    public ServerMultiTenantOptionsAccessor(IOptions<UAuthServerOptions> options)
    {
        _options = options;
    }

    public UAuthMultiTenantOptions MultiTenant => _options.Value.MultiTenant;
}
