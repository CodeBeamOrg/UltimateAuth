using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    internal sealed class InMemoryUserIdentifierStore : IUserIdentifierStore
    {
        private readonly ConcurrentDictionary<(string? TenantId, UserKey UserKey), List<UserIdentifierRecord>> _byUser = new();
        private readonly ConcurrentDictionary<(string? TenantId, UserIdentifierType Type, string Value), UserKey> _lookup = new();

        public Task<IReadOnlyCollection<UserIdentifierRecord>> GetAllAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var key = (tenantId, userKey);

            if (_byUser.TryGetValue(key, out var list))
                return Task.FromResult<IReadOnlyCollection<UserIdentifierRecord>>(list.ToArray());

            return Task.FromResult<IReadOnlyCollection<UserIdentifierRecord>>(Array.Empty<UserIdentifierRecord>());
        }

        public Task<IReadOnlyCollection<UserIdentifierRecord>> GetByTypeAsync(string? tenantId, UserKey userKey, UserIdentifierType type, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var key = (tenantId, userKey);

            if (_byUser.TryGetValue(key, out var list))
            {
                var result = list.Where(x => x.Type == type).ToArray();
                return Task.FromResult<IReadOnlyCollection<UserIdentifierRecord>>(result);
            }

            return Task.FromResult<IReadOnlyCollection<UserIdentifierRecord>>(Array.Empty<UserIdentifierRecord>());
        }

        public Task SetAsync(string? tenantId, UserKey userKey, UserIdentifierRecord record, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var userKeyTuple = (tenantId, userKey);
            var lookupKey = (tenantId, record.Type, record.Value);

            var list = _byUser.GetOrAdd(userKeyTuple, _ => new List<UserIdentifierRecord>());

            // replace if same type+value exists
            var existingIndex = list.FindIndex(x => x.Type == record.Type && StringComparer.OrdinalIgnoreCase.Equals(x.Value, record.Value));

            if (existingIndex >= 0)
            {
                list[existingIndex] = record;
            }
            else
            {
                list.Add(record);
                _lookup[lookupKey] = userKey;
            }

            return Task.CompletedTask;
        }

        public Task MarkVerifiedAsync(string? tenantId, UserKey userKey, UserIdentifierType type, DateTimeOffset verifiedAt, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var key = (tenantId, userKey);

            if (!_byUser.TryGetValue(key, out var list))
                return Task.CompletedTask;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type == type && !list[i].VerifiedAt.HasValue)
                {
                    list[i] = list[i] with
                    {
                        VerifiedAt = verifiedAt
                    };
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string? tenantId, UserIdentifierType type, string value, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var key = (tenantId, type, value);
            return Task.FromResult(_lookup.ContainsKey(key));
        }

        public Task DeleteAsync(string? tenantId, UserKey userKey, UserIdentifierType type, string value, DeleteMode mode, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var userKeyTuple = (tenantId, userKey);
            var lookupKey = (tenantId, type, value);

            if (!_byUser.TryGetValue(userKeyTuple, out var list))
                return Task.CompletedTask;

            var index = list.FindIndex(x => x.Type == type && StringComparer.OrdinalIgnoreCase.Equals(x.Value, value));

            if (index < 0)
                return Task.CompletedTask;

            var record = list[index];

            if (mode == DeleteMode.Soft)
            {
                if (record.DeletedAt.HasValue)
                    return Task.CompletedTask;

                list[index] = record with
                {
                    DeletedAt = DateTimeOffset.UtcNow
                };
            }
            else
            {
                list.RemoveAt(index);
                _lookup.TryRemove(lookupKey, out _);
            }

            return Task.CompletedTask;
        }
    }
}
