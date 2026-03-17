using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

internal sealed class EfCoreAuthenticationSecurityStateStore : IAuthenticationSecurityStateStore
{
    private readonly UAuthAuthenticationDbContext _db;

    public EfCoreAuthenticationSecurityStateStore(UAuthAuthenticationDbContext db)
    {
        _db = db;
    }

    public async Task<AuthenticationSecurityState?> GetAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default)
    {
        var entity = await _db.AuthenticationSecurityStates
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey &&
                x.Scope == scope &&
                x.CredentialType == credentialType,
                ct);

        return entity is null ? null : AuthenticationSecurityStateMapper.ToDomain(entity);
    }

    public async Task AddAsync(AuthenticationSecurityState state, CancellationToken ct = default)
    {
        var entity = AuthenticationSecurityStateMapper.ToProjection(state);

        _db.AuthenticationSecurityStates.Add(entity);

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AuthenticationSecurityState state, long expectedVersion, CancellationToken ct = default)
    {
        var entity = await _db.AuthenticationSecurityStates
            .SingleOrDefaultAsync(x =>
                x.Id == state.Id,
                ct);

        if (entity is null)
            throw new UAuthNotFoundException("security_state_not_found");

        if (entity.SecurityVersion != expectedVersion)
            throw new UAuthConflictException("security_state_version_conflict");

        AuthenticationSecurityStateMapper.UpdateProjection(state, entity);
        entity.SecurityVersion++;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default)
    {
        var entity = await _db.AuthenticationSecurityStates
            .SingleOrDefaultAsync(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey &&
                x.Scope == scope &&
                x.CredentialType == credentialType,
                ct);

        if (entity is null)
            return;

        _db.AuthenticationSecurityStates.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
