using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IUAuthMultiTenantOptionsAccessor
{
    UAuthMultiTenantOptions MultiTenant { get; }
}