using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore;

internal sealed class FailingUserIdentifierStoreFactory
    : IUserIdentifierStoreFactory
{
    private readonly IUserIdentifierStoreFactory _inner;
    private readonly UserIdentifierStoreFaultState _fault;

    public FailingUserIdentifierStoreFactory(
        IUserIdentifierStoreFactory inner,
        UserIdentifierStoreFaultState fault)
    {
        _inner = inner;
        _fault = fault;
    }

    public IUserIdentifierStore Create(TenantKey tenant)
    {
        return new FailingUserIdentifierStore(
            _inner.Create(tenant),
            _fault);
    }
}
