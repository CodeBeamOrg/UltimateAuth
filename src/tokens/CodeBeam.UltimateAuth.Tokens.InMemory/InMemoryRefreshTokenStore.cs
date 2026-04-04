using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

internal sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly TenantKey _tenant;
    private readonly SemaphoreSlim _tx = new(1, 1);

    private readonly ConcurrentDictionary<(TenantKey, string), RefreshToken> _tokens = new();

    public InMemoryRefreshTokenStore(TenantKey tenant)
    {
        _tenant = tenant;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await _tx.WaitAsync(ct);

        try
        {
            await action(ct);
        }
        finally
        {
            _tx.Release();
        }
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        await _tx.WaitAsync(ct);

        try
        {
            return await action(ct);
        }
        finally
        {
            _tx.Release();
        }
    }

    public Task StoreAsync(RefreshToken token, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (token.Tenant != _tenant)
            throw new InvalidOperationException("Tenant mismatch.");

        _tokens[(_tenant, token.TokenHash)] = token;

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _tokens.TryGetValue((_tenant, tokenHash), out var token);

        return Task.FromResult(token);
    }

    public Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_tokens.TryGetValue((_tenant, tokenHash), out var token) && !token.IsRevoked)
        {
            _tokens[(_tenant, tokenHash)] = token.Revoke(revokedAt, replacedByTokenHash);
        }

        return Task.CompletedTask;
    }

    public Task RevokeBySessionAsync(AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var ((tenant, hash), token) in _tokens.ToArray())
        {
            if (tenant != _tenant)
                continue;

            if (token.SessionId == sessionId && !token.IsRevoked)
            {
                _tokens[(_tenant, hash)] = token.Revoke(revokedAt);
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeByChainAsync(SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var ((tenant, hash), token) in _tokens.ToArray())
        {
            if (tenant != _tenant)
                continue;

            if (token.ChainId == chainId && !token.IsRevoked)
            {
                _tokens[(_tenant, hash)] = token.Revoke(revokedAt);
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var ((tenant, hash), token) in _tokens.ToArray())
        {
            if (tenant != _tenant)
                continue;

            if (token.UserKey == userKey && !token.IsRevoked)
            {
                _tokens[(_tenant, hash)] = token.Revoke(revokedAt);
            }
        }

        return Task.CompletedTask;
    }
}
