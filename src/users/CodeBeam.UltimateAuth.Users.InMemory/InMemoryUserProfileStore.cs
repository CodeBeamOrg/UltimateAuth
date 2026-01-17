using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.InMemory.Stores;

internal sealed class InMemoryUserProfileStore : IUserProfileStore
{
    private readonly ConcurrentDictionary<UserKey, InMemoryUserProfile> _profiles = new();

    public Task<ReferenceUserProfile?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_profiles.TryGetValue(userKey, out var profile))
            return Task.FromResult<ReferenceUserProfile?>(null);

        if (profile.IsDeleted)
            return Task.FromResult<ReferenceUserProfile?>(null);

        return Task.FromResult(Map(profile));
    }

    public Task UpdateAsync(string? tenantId, UserKey userKey, UpdateProfileRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var profile = _profiles.GetOrAdd(userKey, key => new InMemoryUserProfile { UserKey = key });

        profile.FirstName = request.FirstName;
        profile.LastName = request.LastName;
        profile.DisplayName = request.DisplayName;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    public Task SetStatusAsync(string? tenantId, UserKey userKey, UserStatus status, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var profile = _profiles.GetOrAdd(
            userKey,
            key => new InMemoryUserProfile { UserKey = key });

        profile.Status = status;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string? tenantId, UserKey userKey, UserDeleteMode mode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (mode == UserDeleteMode.Hard)
        {
            _profiles.TryRemove(userKey, out _);
            return Task.CompletedTask;
        }

        // Soft delete
        var profile = _profiles.GetOrAdd(userKey, key => new InMemoryUserProfile { UserKey = key });

        profile.IsDeleted = true;
        profile.Status = UserStatus.Deleted;
        profile.DeletedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }


    private static ReferenceUserProfile Map(InMemoryUserProfile profile)
        => new()
        {
            UserKey = profile.UserKey,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            Status = profile.Status,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            IsDeleted = profile.IsDeleted,
            DeletedAt = profile.DeletedAt,
        };
}
