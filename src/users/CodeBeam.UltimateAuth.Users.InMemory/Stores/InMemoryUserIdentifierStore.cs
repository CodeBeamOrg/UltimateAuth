using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserIdentifierStore : IUserIdentifierStore
{
    private readonly Dictionary<Guid, UserIdentifier> _store = new();

    public Task<bool> ExistsAsync(TenantKey tenant, UserIdentifierType type, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var exists = _store.Values.Any(x =>
            x.Tenant == tenant &&
            x.Type == type &&
            x.Value == value &&
            !x.IsDeleted);

        return Task.FromResult(exists);
    }

    public Task<UserIdentifier?> GetAsync(TenantKey tenant, UserIdentifierType type, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifier = _store.Values.FirstOrDefault(x =>
            x.Tenant == tenant &&
            x.Type == type &&
            x.Value == value &&
            !x.IsDeleted);

        return Task.FromResult(identifier);
    }

    public Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier))
            return Task.FromResult<UserIdentifier?>(null);

        if (identifier.IsDeleted)
            return Task.FromResult<UserIdentifier?>(null);

        return Task.FromResult<UserIdentifier?>(identifier);
    }

    public Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = _store.Values
            .Where(x => x.Tenant == tenant)
            .Where(x => x.UserKey == userKey)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<UserIdentifier>>(result);
    }

    public Task CreateAsync(TenantKey tenant, UserIdentifier identifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (identifier.Id == Guid.Empty)
            identifier.Id = Guid.NewGuid();

        var duplicate = _store.Values.Any(x =>
            x.Tenant == tenant &&
            x.Type == identifier.Type &&
            x.Value == identifier.Value &&
            !x.IsDeleted);

        if (duplicate)
            throw new UAuthConflictException("identifier_already_exists");

        identifier.Tenant = tenant;

        _store[identifier.Id] = identifier;

        return Task.CompletedTask;
    }

    public Task UpdateValueAsync(Guid id, string newValue, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier) || identifier.IsDeleted)
            throw new InvalidOperationException("identifier_not_found");

        if (identifier.Value == newValue)
            throw new InvalidOperationException("identifier_value_unchanged");

        var duplicate = _store.Values.Any(x =>
            x.Id != id &&
            x.Tenant == identifier.Tenant &&
            x.Type == identifier.Type &&
            x.Value == newValue &&
            !x.IsDeleted);

        if (duplicate)
            throw new InvalidOperationException("identifier_value_already_exists");

        identifier.Value = newValue;
        identifier.IsVerified = false;
        identifier.VerifiedAt = null;
        identifier.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task MarkVerifiedAsync(Guid id, DateTimeOffset verifiedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier) || identifier.IsDeleted)
            throw new InvalidOperationException("identifier_not_found");

        if (identifier.IsVerified)
            return Task.CompletedTask;

        identifier.IsVerified = true;
        identifier.VerifiedAt = verifiedAt;
        identifier.UpdatedAt = verifiedAt;

        return Task.CompletedTask;
    }

    public Task SetPrimaryAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var target) || target.IsDeleted)
            throw new InvalidOperationException("identifier_not_found");

        foreach (var idf in _store.Values.Where(x =>
                     x.Tenant == target.Tenant &&
                     x.UserKey == target.UserKey &&
                     x.Type == target.Type &&
                     x.IsPrimary))
        {
            idf.IsPrimary = false;
        }

        target.IsPrimary = true;

        return Task.CompletedTask;
    }

    public Task UnsetPrimaryAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier) || identifier.IsDeleted)
            throw new InvalidOperationException("identifier_not_found");

        identifier.IsPrimary = false;
        identifier.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier))
            return Task.CompletedTask;

        if (mode == DeleteMode.Hard)
        {
            _store.Remove(id);
            return Task.CompletedTask;
        }

        if (identifier.IsDeleted)
            return Task.CompletedTask;

        identifier.IsDeleted = true;
        identifier.DeletedAt = deletedAt;
        identifier.IsPrimary = false;
        identifier.UpdatedAt = deletedAt;

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifiers = _store.Values.Where(x => x.Tenant == tenant && x.UserKey == userKey).ToList();

        foreach (var identifier in identifiers)
        {
            if (mode == DeleteMode.Hard)
            {
                _store.Remove(identifier.Id);
            }
            else
            {
                if (identifier.IsDeleted)
                    continue;

                identifier.IsDeleted = true;
                identifier.DeletedAt = deletedAt;
                identifier.IsPrimary = false;
            }
        }

        return Task.CompletedTask;
    }
}
