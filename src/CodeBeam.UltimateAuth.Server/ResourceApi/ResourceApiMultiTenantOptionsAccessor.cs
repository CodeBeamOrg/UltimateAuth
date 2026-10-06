using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class ResourceApiMultiTenantOptionsAccessor : IUAuthMultiTenantOptionsAccessor
{
    private readonly IOptions<UAuthResourceApiOptions> _options;

    public ResourceApiMultiTenantOptionsAccessor(IOptions<UAuthResourceApiOptions> options)
    {
        _options = options;
    }

    public UAuthMultiTenantOptions MultiTenant => _options.Value.MultiTenant;
}
