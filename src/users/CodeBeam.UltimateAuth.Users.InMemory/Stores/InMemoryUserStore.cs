using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserStore : IUserStore<UserKey>
{
    private readonly ConcurrentDictionary<UserKey, ReferenceUserProfile> _users;
    private readonly ConcurrentDictionary<string, UserKey> _userKeysByLogin;

    public InMemoryUserStore(IEnumerable<ReferenceUserProfile>? seededUsers = null)
    {
        _users = new ConcurrentDictionary<UserKey, ReferenceUserProfile>();
        _userKeysByLogin = new ConcurrentDictionary<string, UserKey>(StringComparer.OrdinalIgnoreCase);

        if (seededUsers is null)
            return;

        foreach (var user in seededUsers)
        {
            _users[user.UserKey] = user;

            if (!string.IsNullOrWhiteSpace(user.Email))
                _userKeysByLogin[user.Email] = user.UserKey;
        }
    }

    public Task<AuthUserRecord<UserKey>?> FindByIdAsync(string? tenantId, UserKey userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_users.TryGetValue(userId, out var user))
            return Task.FromResult<AuthUserRecord<UserKey>?>(null);

        if (user.IsDeleted)
            return Task.FromResult<AuthUserRecord<UserKey>?>(null);

        return Task.FromResult<AuthUserRecord<UserKey>?>(Map(user));
    }

    public Task<AuthUserRecord<UserKey>?> FindByLoginAsync(string? tenantId, string login, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_userKeysByLogin.TryGetValue(login, out var userKey))
            return Task.FromResult<AuthUserRecord<UserKey>?>(null);

        if (!_users.TryGetValue(userKey, out var user))
            return Task.FromResult<AuthUserRecord<UserKey>?>(null);

        if (user.IsDeleted)
            return Task.FromResult<AuthUserRecord<UserKey>?>(null);

        return Task.FromResult<AuthUserRecord<UserKey>?>(Map(user));
    }

    public Task<bool> ExistsAsync(string? tenantId, UserKey userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(
            _users.TryGetValue(userId, out var user) && !user.IsDeleted);
    }

    private static AuthUserRecord<UserKey> Map(ReferenceUserProfile user)
    {
        return new AuthUserRecord<UserKey>
        {
            Id = user.UserKey,
            Identifier = user.Email ?? user.DisplayName ?? string.Empty,
            IsActive = user.Status == UserStatus.Active,
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt,
            DeletedAt = user.DeletedAt
        };
    }
}
