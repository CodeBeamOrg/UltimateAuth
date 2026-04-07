using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.InMemory;

public abstract class InMemoryTenantVersionedStore<TEntity, TKey> : InMemoryVersionedStore<TEntity, TKey>
    where TEntity : class, IVersionedEntity, IEntitySnapshot<TEntity>, ITenantEntity
    where TKey : notnull, IEquatable<TKey>
{
    private readonly TenantExecutionContext _tenant;

    protected InMemoryTenantVersionedStore(TenantExecutionContext tenant)
    {
        _tenant = tenant;
    }

    protected override void BeforeAdd(TEntity entity)
    {
        EnsureTenant(entity);
        base.BeforeAdd(entity);
    }

    protected override void BeforeSave(TEntity entity, TEntity current, long expectedVersion)
    {
        EnsureTenant(entity);
        base.BeforeSave(entity, current, expectedVersion);
    }

    protected override void BeforeDelete(TEntity current, long expectedVersion, DeleteMode mode, DateTimeOffset now)
    {
        EnsureTenant(current);
        base.BeforeDelete(current, expectedVersion, mode, now);
    }

    protected IReadOnlyList<TEntity> TenantValues()
    {
        return InternalValues()
            .Where(x => x.Tenant == _tenant.Tenant)
            .Select(Snapshot)
            .ToList()
            .AsReadOnly();
    }

    private void EnsureTenant(TEntity entity)
    {
        if (!_tenant.IsGlobal && entity.Tenant != _tenant.Tenant)
            throw new UAuthConflictException("tenant_mismatch");
    }
}
