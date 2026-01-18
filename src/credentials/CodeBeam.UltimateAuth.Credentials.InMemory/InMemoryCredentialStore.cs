using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Credentials.Reference;
using System.Collections.Concurrent;

internal sealed class InMemoryCredentialStore<TUserId> : ICredentialStore<TUserId>, ICredentialSecretStore<TUserId>, ICredentialSecurityVersionStore<TUserId>
    where TUserId : notnull
{
    private readonly ConcurrentDictionary<string, InMemoryPasswordCredentialState<TUserId>> _byLogin;
    private readonly ConcurrentDictionary<TUserId, List<InMemoryPasswordCredentialState<TUserId>>> _byUser;

    private readonly IUAuthPasswordHasher _hasher;
    private readonly IInMemoryUserIdProvider<TUserId> _userIdProvider;

    public InMemoryCredentialStore(IUAuthPasswordHasher hasher, IInMemoryUserIdProvider<TUserId> userIdProvider)
    {
        _hasher = hasher;
        _userIdProvider = userIdProvider;

        _byLogin = new ConcurrentDictionary<string, InMemoryPasswordCredentialState<TUserId>>(
            StringComparer.OrdinalIgnoreCase);

        _byUser = new ConcurrentDictionary<TUserId, List<InMemoryPasswordCredentialState<TUserId>>>();

        SeedDefault();
    }

    private void SeedDefault()
    {
        SeedUser("admin", _userIdProvider.GetAdminUserId());
        SeedUser("user", _userIdProvider.GetUserUserId());
    }

    private void SeedUser(string login, TUserId userId)
    {
        var state = new InMemoryPasswordCredentialState<TUserId>
        {
            UserId = userId,
            Login = login,
            SecretHash = _hasher.Hash(login), // admin/admin, user/user
            Status = CredentialStatus.Active,
            SecurityVersion = 0,
            Metadata = new CredentialMetadata(
                CreatedAt: DateTimeOffset.UtcNow,
                LastUsedAt: null,
                Source: "seed")
        };

        _byLogin[login] = state;
        _byUser[userId] = new List<InMemoryPasswordCredentialState<TUserId>> { state };
    }

    public Task<ICredential<TUserId>?> FindByLoginAsync(string? tenantId, string loginIdentifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byLogin.TryGetValue(loginIdentifier, out var state))
            return Task.FromResult<ICredential<TUserId>?>(null);

        if (state.Status != CredentialStatus.Active)
            return Task.FromResult<ICredential<TUserId>?>(null);

        return Task.FromResult<ICredential<TUserId>?>(Map(state));
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAsync(string? tenantId, TUserId userId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue(userId, out var list))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        var active = list
            .Where(c => c.Status == CredentialStatus.Active)
            .Select(Map)
            .Cast<ICredential<TUserId>>()
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(active);
    }

    public Task UpdateSecretAsync(string? tenantId, TUserId userId, CredentialType type, string newSecretHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue(userId, out var list))
            return Task.CompletedTask;

        var state = list.FirstOrDefault(c => c.Type == type);
        if (state is null)
            return Task.CompletedTask;

        state.SecretHash = newSecretHash;
        state.SecurityVersion++;
        state.Metadata = state.Metadata with
        {
            LastUsedAt = DateTimeOffset.UtcNow
        };

        return Task.CompletedTask;
    }

    // Security version
    public Task<long> GetAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        if (!_byUser.TryGetValue(userId, out var list))
            return Task.FromResult(0L);

        return Task.FromResult(list.Max(c => c.SecurityVersion));
    }

    public Task IncrementAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        if (_byUser.TryGetValue(userId, out var list))
        {
            foreach (var c in list)
                c.SecurityVersion++;
        }

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryRemove(userId, out var list))
        {
            foreach (var credential in list)
            {
                _byLogin.TryRemove(credential.Login, out _);
            }
        }

        return Task.CompletedTask;
    }

    private static PasswordCredential<TUserId> Map(
        InMemoryPasswordCredentialState<TUserId> state)
        => new(
            userId: state.UserId,
            loginIdentifier: state.Login,
            secretHash: state.SecretHash,
            status: state.Status,
            metadata: state.Metadata);
}
