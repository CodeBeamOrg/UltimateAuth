using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserLifecycleStore : IUserLifecycleStore
{
    private readonly ConcurrentDictionary<UserIdentity, ReferenceUserProfile> _users = new();
    private readonly IInMemoryUserIdProvider<UserKey> _idProvider;
    private readonly IClock _clock;

    public InMemoryUserLifecycleStore(IInMemoryUserIdProvider<UserKey> idProvider, IClock clock)
    {
        _idProvider = idProvider;
        _clock = clock;
        SeedDefault();
    }

    private void SeedDefault()
    {
        CreateSeedUser(_idProvider.GetAdminUserId(), "admin");
        CreateSeedUser(_idProvider.GetUserUserId(), "user");
    }

    private void CreateSeedUser(UserKey userKey, string identifier)
    {
        var now = _clock.UtcNow;

        var profile = new ReferenceUserProfile
        {
            UserKey = userKey,
            Email = identifier,
            DisplayName = identifier == "admin"
                ? "Administrator"
                : "Standard User",
            Status = UserStatus.Active,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            DeletedAt = null
        };

        _users.TryAdd(
            new UserIdentity(null, userKey),
            profile);
    }

    public Task CreateAsync(string? tenantId, ReferenceUserProfile user, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(user);

        var identity = new UserIdentity(tenantId, user.UserKey);

        if (!_users.TryAdd(identity, InitializeUser(user)))
        {
            throw new InvalidOperationException(
                $"User '{user.UserKey}' already exists in tenant '{tenantId ?? "<default>"}'.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(
        string? tenantId,
        UserKey userKey,
        UserStatus status,
        CancellationToken ct = default)
    {
        var identity = new UserIdentity(tenantId, userKey);

        if (!_users.TryGetValue(identity, out var user))
            throw new InvalidOperationException($"User '{userKey}' does not exist.");

        if (user.IsDeleted)
            throw new InvalidOperationException($"User '{userKey}' is deleted.");

        if (user.Status == status)
            return Task.CompletedTask; // idempotent

        if (!IsValidStatusTransition(user.Status, status))
            throw new InvalidOperationException(
                $"Invalid status transition from '{user.Status}' to '{status}'.");

        user.Status = status;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identity = new UserIdentity(tenantId, userKey);

        if (!_users.TryGetValue(identity, out var user))
        {
            return Task.CompletedTask;
        }

        switch (mode)
        {
            case DeleteMode.Soft:
                if (user.IsDeleted)
                    return Task.CompletedTask;

                user.Status = UserStatus.Deleted;
                user.IsDeleted = true;
                user.DeletedAt = at;
                user.UpdatedAt = at;
                break;

            case DeleteMode.Hard:
                _users.TryRemove(identity, out _);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown delete mode.");
        }

        return Task.CompletedTask;
    }

    private static ReferenceUserProfile InitializeUser(ReferenceUserProfile user)
    {
        return user with
        {
            Status = user.Status == default ? UserStatus.Active : user.Status,
            CreatedAt = user.CreatedAt == default ? DateTimeOffset.UtcNow : user.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    private static bool IsValidStatusTransition(UserStatus from, UserStatus to)
    {
        return from switch
        {
            UserStatus.Active => to is UserStatus.Suspended or UserStatus.Disabled,
            UserStatus.Suspended => to is UserStatus.Active or UserStatus.Disabled,
            UserStatus.Disabled => to is UserStatus.Active,
            _ => false
        };
    }

    private readonly record struct UserIdentity(string? TenantId, UserKey UserKey);
}
