using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    public sealed class InMemoryUserIdentifierStore : IUserIdentifierStore
    {
        private readonly Dictionary<(string? TenantId, UserIdentifierType Type, string Value), UserIdentifier> _store = new();

        public Task<bool> ExistsAsync(string? tenantId, UserIdentifierType type, string value, CancellationToken ct = default)
        {
            return Task.FromResult(_store.TryGetValue((tenantId, type, value), out var id) && !id.IsDeleted);
        }

        public Task<UserIdentifier?> GetAsync(string? tenantId, UserIdentifierType type, string value, CancellationToken ct = default)
        {
            if (!_store.TryGetValue((tenantId, type, value), out var id))
                return Task.FromResult<UserIdentifier?>(null);

            if (id.IsDeleted)
                return Task.FromResult<UserIdentifier?>(null);

            return Task.FromResult<UserIdentifier?>(id);
        }

        public Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            var result = _store.Values
                .Where(x => x.TenantId == tenantId)
                .Where(x => x.UserKey == userKey)
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.CreatedAt)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<UserIdentifier>>(result);
        }

        public Task CreateAsync(string? tenantId, UserIdentifier identifier, CancellationToken ct = default)
        {
            var key = (tenantId, identifier.Type, identifier.Value);

            if (_store.TryGetValue(key, out var existing) && !existing.IsDeleted)
                throw new InvalidOperationException("Identifier already exists.");

            _store[key] = identifier;
            return Task.CompletedTask;
        }

        public Task UpdateValueAsync(string? tenantId, UserIdentifierType type, string oldValue, string newValue, DateTimeOffset updatedAt, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                throw new InvalidOperationException("identifier_value_unchanged");

            var oldKey = (tenantId, type, oldValue);

            if (!_store.TryGetValue(oldKey, out var identifier) || identifier.IsDeleted)
                throw new InvalidOperationException("identifier_not_found");

            var newKey = (tenantId, type, newValue);

            if (_store.ContainsKey(newKey))
                throw new InvalidOperationException("identifier_value_already_exists");

            _store.Remove(oldKey);

            identifier.Value = newValue;
            identifier.IsVerified = false;
            identifier.VerifiedAt = null;
            identifier.UpdatedAt = updatedAt;

            _store[newKey] = identifier;

            return Task.CompletedTask;
        }

        public Task MarkVerifiedAsync(string? tenantId, UserIdentifierType type, string value, DateTimeOffset verifiedAt, CancellationToken ct = default)
        {
            var key = (tenantId, type, value);

            if (!_store.TryGetValue(key, out var id) || id.IsDeleted)
                throw new InvalidOperationException("Identifier not found.");

            if (id.IsVerified)
                return Task.CompletedTask;

            id.IsVerified = true;
            id.VerifiedAt = verifiedAt;

            return Task.CompletedTask;
        }

        public Task SetPrimaryAsync(string? tenantId, UserKey userKey, UserIdentifierType type, string value, CancellationToken ct = default)
        {
            foreach (var id in _store.Values.Where(x =>
                         x.TenantId == tenantId &&
                         x.UserKey == userKey &&
                         x.Type == type &&
                         !x.IsDeleted &&
                         x.IsPrimary))
            {
                id.IsPrimary = false;
            }

            var key = (tenantId, type, value);

            if (!_store.TryGetValue(key, out var target) || target.IsDeleted)
                throw new InvalidOperationException("Identifier not found.");

            target.IsPrimary = true;
            return Task.CompletedTask;
        }

        public Task UnsetPrimaryAsync(string? tenantId, UserKey userKey, UserIdentifierType type, string value, CancellationToken ct = default)
        {
            var key = (tenantId, type, value);

            if (!_store.TryGetValue(key, out var target) || target.IsDeleted)
                throw new InvalidOperationException("Identifier not found.");

            target.IsPrimary = false;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string? tenantId, UserIdentifierType type, string value, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
        {
            var key = (tenantId, type, value);

            if (!_store.TryGetValue(key, out var id))
                return Task.CompletedTask;

            if (mode == DeleteMode.Hard)
            {
                _store.Remove(key);
                return Task.CompletedTask;
            }

            if (id.IsDeleted)
                return Task.CompletedTask;

            id.IsDeleted = true;
            id.DeletedAt = deletedAt;
            id.IsPrimary = false;

            return Task.CompletedTask;
        }

        public Task DeleteByUserAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
        {
            var identifiers = _store.Values
                .Where(x => x.TenantId == tenantId)
                .Where(x => x.UserKey == userKey)
                .ToList();

            foreach (var id in identifiers)
            {
                if (mode == DeleteMode.Hard)
                {
                    _store.Remove((tenantId, id.Type, id.Value));
                }
                else
                {
                    if (id.IsDeleted)
                        continue;

                    id.IsDeleted = true;
                    id.DeletedAt = deletedAt;
                    id.IsPrimary = false;
                }
            }

            return Task.CompletedTask;
        }

    }
}
