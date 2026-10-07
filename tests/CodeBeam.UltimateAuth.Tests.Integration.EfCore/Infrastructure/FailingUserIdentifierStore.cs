using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore;

internal sealed class FailingUserIdentifierStore : IUserIdentifierStore
{
    private readonly IUserIdentifierStore _inner;
    private readonly UserIdentifierStoreFaultState _fault;

    public FailingUserIdentifierStore(IUserIdentifierStore inner, UserIdentifierStoreFaultState fault)
    {
        _inner = inner;
        _fault = fault;
    }

    public Task<UserIdentifier?> GetAsync(
        Guid key,
        CancellationToken ct = default)
        => _inner.GetAsync(key, ct);

    public Task<bool> ExistsAsync(
        Guid key,
        CancellationToken ct = default)
        => _inner.ExistsAsync(key, ct);

    public async Task AddAsync(
        UserIdentifier entity,
        CancellationToken ct = default)
    {
        if (_fault.ShouldFail(entity))
        {
            throw new InvalidOperationException(
                "simulated_identifier_store_failure");
        }

        await _inner.AddAsync(entity, ct);
    }

    public Task SaveAsync(
        UserIdentifier entity,
        long expectedVersion,
        CancellationToken ct = default)
        => _inner.SaveAsync(entity, expectedVersion, ct);

    public Task DeleteAsync(
        Guid key,
        long expectedVersion,
        DeleteMode deleteMode,
        DateTimeOffset now,
        CancellationToken ct = default)
        => _inner.DeleteAsync(
            key,
            expectedVersion,
            deleteMode,
            now,
            ct);

    public Task<IdentifierExistenceResult> ExistsAsync(
        IdentifierExistenceQuery query,
        CancellationToken ct = default)
        => _inner.ExistsAsync(query, ct);

    public Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(
        UserKey userKey,
        CancellationToken ct = default)
        => _inner.GetByUserAsync(userKey, ct);

    public Task<UserIdentifier?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
        => _inner.GetByIdAsync(id, ct);

    public Task<UserIdentifier?> GetAsync(
        UserIdentifierType type,
        string value,
        CancellationToken ct = default)
        => _inner.GetAsync(type, value, ct);

    public Task<PagedResult<UserIdentifier>> QueryAsync(
        UserIdentifierQuery query,
        CancellationToken ct = default)
        => _inner.QueryAsync(query, ct);

    public Task<IReadOnlyList<UserIdentifier>> GetByUsersAsync(
        IReadOnlyList<UserKey> userKeys,
        CancellationToken ct = default)
        => _inner.GetByUsersAsync(userKeys, ct);

    public Task DeleteByUserAsync(
        UserKey userKey,
        DeleteMode mode,
        DateTimeOffset deletedAt,
        CancellationToken ct = default)
        => _inner.DeleteByUserAsync(
            userKey,
            mode,
            deletedAt,
            ct);
}
