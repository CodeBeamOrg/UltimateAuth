using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryCredentialStore<TUserId> : ICredentialStore<TUserId>, ICredentialSecretStore<TUserId> where TUserId : notnull
{
    private readonly ConcurrentDictionary<(TenantKey Tenant, string Login), InMemoryPasswordCredentialState<TUserId>> _byLogin;
    private readonly ConcurrentDictionary<(TenantKey Tenant, TUserId UserId), List<InMemoryPasswordCredentialState<TUserId>>> _byUser;

    private readonly IUAuthPasswordHasher _hasher;
    private readonly IInMemoryUserIdProvider<TUserId> _userIdProvider;

    public InMemoryCredentialStore(IUAuthPasswordHasher hasher, IInMemoryUserIdProvider<TUserId> userIdProvider)
    {
        _hasher = hasher;
        _userIdProvider = userIdProvider;

        _byLogin = new ConcurrentDictionary<(TenantKey, string), InMemoryPasswordCredentialState<TUserId>>();
        _byUser = new ConcurrentDictionary<(TenantKey, TUserId), List<InMemoryPasswordCredentialState<TUserId>>>();
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> FindByLoginAsync(TenantKey tenant, string loginIdentifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byLogin.TryGetValue((tenant, loginIdentifier), out var state))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(new[] { Map(state) });
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue((tenant, userId), out var list))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(list.Select(Map).ToArray());
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAndTypeAsync(TenantKey tenant, TUserId userId, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue((tenant, userId), out var list))
            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(
            list.Where(c => c.Type == type)
                .Select(Map)
                .ToArray());
    }

    public Task<bool> ExistsAsync(TenantKey tenant, TUserId userId, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(_byUser.TryGetValue((tenant, userId), out var list) && list.Any(c => c.Type == type));
    }

    public Task AddAsync(TenantKey tenant, ICredential<TUserId> credential, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (credential is not PasswordCredential<TUserId> pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        var state = new InMemoryPasswordCredentialState<TUserId>
        {
            UserId = pwd.UserId,
            Login = pwd.LoginIdentifier,
            SecretHash = pwd.SecretHash,
            Security = pwd.Security,
            Metadata = pwd.Metadata
        };

        _byLogin[(tenant, pwd.LoginIdentifier)] = state;

        _byUser.AddOrUpdate(
            (tenant, pwd.UserId),
            _ => new List<InMemoryPasswordCredentialState<TUserId>> { state },
            (_, list) =>
            {
                list.Add(state);
                return list;
            });

        return Task.CompletedTask;
    }

    public Task UpdateSecurityStateAsync(TenantKey tenant, TUserId userId, CredentialType type, CredentialSecurityState securityState, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue((tenant, userId), out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
                state.Security = securityState;
        }

        return Task.CompletedTask;
    }

    public Task UpdateMetadataAsync(TenantKey tenant, TUserId userId, CredentialType type, CredentialMetadata metadata, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue((tenant, userId), out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
                state.Metadata = metadata;
        }

        return Task.CompletedTask;
    }

    public Task SetAsync(TenantKey tenant, TUserId userId, CredentialType type, string secretHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue((tenant, userId), out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
                state.SecretHash = secretHash;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantKey tenant, TUserId userId, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryGetValue((tenant, userId), out var list))
        {
            var state = list.FirstOrDefault(c => c.Type == type);
            if (state != null)
            {
                list.Remove(state);
                _byLogin.TryRemove((tenant, state.Login), out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_byUser.TryRemove((tenant, userId), out var list))
        {
            foreach (var credential in list)
                _byLogin.TryRemove((tenant, credential.Login), out _);
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
