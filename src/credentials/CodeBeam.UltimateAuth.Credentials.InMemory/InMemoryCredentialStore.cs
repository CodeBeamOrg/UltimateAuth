using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Credentials.Reference;
using System.Collections.Concurrent;

internal sealed class InMemoryCredentialStore<TUserId> : ICredentialStore<TUserId>, ICredentialSecretStore<TUserId> where TUserId : notnull
{
    private readonly ConcurrentDictionary<string, InMemoryPasswordCredentialState<TUserId>> _byLogin;
    private readonly ConcurrentDictionary<TUserId, List<InMemoryPasswordCredentialState<TUserId>>> _byUser;

    private readonly IUAuthPasswordHasher _hasher;
    private readonly IInMemoryUserIdProvider<TUserId> _userIdProvider;

    public InMemoryCredentialStore(IUAuthPasswordHasher hasher, IInMemoryUserIdProvider<TUserId> userIdProvider)
    {
        _hasher = hasher;
        _userIdProvider = userIdProvider;

        _byLogin = new ConcurrentDictionary<string, InMemoryPasswordCredentialState<TUserId>>(StringComparer.OrdinalIgnoreCase);
        _byUser = new ConcurrentDictionary<TUserId, List<InMemoryPasswordCredentialState<TUserId>>>();

        //SeedDefault();
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
            SecretHash = _hasher.Hash(login),
            Security = new CredentialSecurityState(CredentialSecurityStatus.Active),
            Metadata = new CredentialMetadata(DateTimeOffset.UtcNow, null, "seed")
        };

        _byLogin[login] = state;
        _byUser[userId] = new List<InMemoryPasswordCredentialState<TUserId>> { state };
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> FindByLoginAsync(string? tenantId, string loginIdentifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byLogin.TryGetValue(loginIdentifier, out var state))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(new[] { Map(state) });
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue(userId, out var list))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(list.Select(Map).Cast<ICredential<TUserId>>().ToArray());
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAndTypeAsync(string? tenantId, TUserId userId, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue(userId, out var list))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(
            list.Where(c => c.Type == type)
                .Select(Map)
                .Cast<ICredential<TUserId>>()
                .ToArray());
    }

    public Task<bool> ExistsAsync(string? tenantId, TUserId userId, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(_byUser.TryGetValue(userId, out var list) && list.Any(c => c.Type == type));
    }

    public Task AddAsync(string? tenantId, ICredential<TUserId> credential, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (credential is not PasswordCredential<TUserId> pwd)
            throw new NotSupportedException("Only password credential supported in-memory.");

        var state = new InMemoryPasswordCredentialState<TUserId>
        {
            UserId = pwd.UserId,
            Login = pwd.LoginIdentifier,
            SecretHash = pwd.SecretHash,
            Security = pwd.Security,
            Metadata = pwd.Metadata
        };

        _byLogin[pwd.LoginIdentifier] = state;
        _byUser.AddOrUpdate(pwd.UserId,
            _ => new List<InMemoryPasswordCredentialState<TUserId>> { state },
            (_, list) =>
            {
                list.Add(state);
                return list;
            });

        return Task.CompletedTask;
    }

    public Task UpdateSecurityStateAsync(string? tenantId, TUserId userId, CredentialType type, CredentialSecurityState securityState, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue(userId, out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
                state.Security = securityState;
        }

        return Task.CompletedTask;
    }

    public Task UpdateMetadataAsync(string? tenantId, TUserId userId, CredentialType type, CredentialMetadata metadata, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue(userId, out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
                state.Metadata = metadata;
        }

        return Task.CompletedTask;
    }

    public Task SetAsync(string? tenantId, TUserId userId, CredentialType type, string secretHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue(userId, out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
                state.SecretHash = secretHash;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string? tenantId, TUserId userId, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue(userId, out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
            {
                list.Remove(state);
                _byLogin.TryRemove(state.Login, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryRemove(userId, out var list))
        {
            foreach (var credential in list)
                _byLogin.TryRemove(credential.Login, out _);
        }

        return Task.CompletedTask;
    }

    private static PasswordCredential<TUserId> Map(InMemoryPasswordCredentialState<TUserId> state)
        => new(
            userId: state.UserId,
            loginIdentifier: state.Login,
            secretHash: state.SecretHash,
            security: state.Security,
            metadata: state.Metadata);
}
