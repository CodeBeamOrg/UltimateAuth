using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryPasswordHistoryStore(TenantKey tenant, InMemoryAtomicContextAccessor atomicContext) : IPasswordHistoryStore
{
    private readonly ConcurrentDictionary<Guid, PasswordHistoryEntry> _entries = new();

    public Task<IReadOnlyList<PasswordHistoryEntry>> GetRecentAsync(CredentialKey credentialKey, int count, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateKey(credentialKey);

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        IReadOnlyList<PasswordHistoryEntry> result = _entries.Values
            .Where(x => x.CredentialKey == credentialKey)
            .OrderByDescending(x => x.ReplacedAt)
            .ThenByDescending(x => x.Id)
            .Take(count)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task AddAsync(PasswordHistoryEntry entry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entry);

        ValidateKey(entry.CredentialKey);

        if (entry.Tenant != tenant)
            throw new UAuthConflictException("tenant_mismatch");

        if (entry.Id == Guid.Empty)
            throw new ArgumentException("History entry ID is required.");

        if (!_entries.TryAdd(entry.Id, entry))
            throw new UAuthConflictException("password_history_exists");

        atomicContext.Current?.RegisterRollback(() =>
        {
            if (!_entries.TryRemove(new KeyValuePair<Guid, PasswordHistoryEntry>(entry.Id, entry)))
            {
                throw new InvalidOperationException("Password history rollback failed.");
            }
        });

        return Task.CompletedTask;
    }

    public Task PruneAsync(CredentialKey credentialKey, int keepLast, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateKey(credentialKey);

        if (keepLast < 0)
            throw new ArgumentOutOfRangeException(nameof(keepLast));

        var remove = _entries.Values
            .Where(x => x.CredentialKey == credentialKey)
            .OrderByDescending(x => x.ReplacedAt)
            .ThenByDescending(x => x.Id)
            .Skip(keepLast)
            .ToArray();

        RemoveEntries(remove);

        return Task.CompletedTask;
    }

    public Task DeleteByCredentialAsync(CredentialKey credentialKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateKey(credentialKey);

        var remove = _entries.Values
            .Where(x => x.CredentialKey == credentialKey)
            .ToArray();

        RemoveEntries(remove);

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var entries = _entries.Values
            .Where(x => x.UserKey == userKey)
            .ToArray();

        RemoveEntries(entries);

        return Task.CompletedTask;
    }

    private void RemoveEntries(IReadOnlyCollection<PasswordHistoryEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (!_entries.TryRemove(new KeyValuePair<Guid, PasswordHistoryEntry>(entry.Id, entry)))
            {
                throw new InvalidOperationException("Password history entry changed concurrently.");
            }

            atomicContext.Current?.RegisterRollback(() =>
            {
                if (!_entries.TryAdd(entry.Id, entry))
                {
                    throw new InvalidOperationException("Password history rollback failed.");
                }
            });
        }
    }

    private void ValidateKey(CredentialKey key)
    {
        if (key.Tenant != tenant)
            throw new UAuthConflictException("tenant_mismatch");
    }
}
