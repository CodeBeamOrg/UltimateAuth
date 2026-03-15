using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

public sealed class EfCoreRefreshTokenStoreFactory : IRefreshTokenStoreFactory
{
    private readonly IServiceProvider _sp;

    public EfCoreRefreshTokenStoreFactory(IServiceProvider sp)
    {
        _sp = sp;
    }

    public IRefreshTokenStore Create(TenantKey tenant)
    {
        return ActivatorUtilities.CreateInstance<EfCoreRefreshTokenStore>(_sp, tenant);
    }
}
