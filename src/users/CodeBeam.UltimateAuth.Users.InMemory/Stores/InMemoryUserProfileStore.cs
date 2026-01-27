using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Domain;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserProfileStore : IUserProfileStore
{
    private readonly ConcurrentDictionary<UserKey, ReferenceUserProfile> _profiles = new();
    private readonly IInMemoryUserIdProvider<UserKey> _idProvider;
    private readonly IClock _clock;

    internal IEnumerable<ReferenceUserProfile> AllProfiles => _profiles.Values;

    public InMemoryUserProfileStore(IInMemoryUserIdProvider<UserKey> idProvider, IClock clock)
    {
        _idProvider = idProvider;
        _clock = clock;
        SeedDefault();
    }

    private void SeedDefault()
    {
        SeedProfile(_idProvider.GetAdminUserId(), "Administrator");
        SeedProfile(_idProvider.GetUserUserId(), "Standard User");
    }

    private void SeedProfile(UserKey userKey, string displayName)
    {
        var now = _clock.UtcNow;

        _profiles[userKey] = new ReferenceUserProfile
        {
            UserKey = userKey,
            DisplayName = displayName,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public Task CreateAsync(string? tenantId, ReferenceUserProfile profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var mem = new ReferenceUserProfile
        {
            UserKey = profile.UserKey,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            Status = profile.Status,
            IsDeleted = profile.IsDeleted,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            DeletedAt = profile.DeletedAt
        };

        if (!_profiles.TryAdd(profile.UserKey, mem))
            throw new InvalidOperationException($"User profile '{profile.UserKey}' already exists.");

        return Task.CompletedTask;
    }

    public Task<ReferenceUserProfile?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_profiles.TryGetValue(userKey, out var profile) || profile.IsDeleted)
            return Task.FromResult<ReferenceUserProfile?>(null);

        return Task.FromResult(Map(profile));
    }

    public Task UpdateAsync(string? tenantId, UserKey userKey, UpdateProfileRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_profiles.TryGetValue(userKey, out var profile) || profile.IsDeleted)
            throw new InvalidOperationException("User profile does not exist.");

        profile.FirstName = request.FirstName;
        profile.LastName = request.LastName;
        profile.DisplayName = request.DisplayName;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (mode == DeleteMode.Hard)
        {
            _profiles.TryRemove(userKey, out _);
            return Task.CompletedTask;
        }

        if (!_profiles.TryGetValue(userKey, out var profile))
            throw new InvalidOperationException("User profile does not exist.");

        profile.IsDeleted = true;
        profile.Status = UserStatus.Deleted;
        profile.DeletedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = profile.DeletedAt;

        return Task.CompletedTask;
    }

    private static ReferenceUserProfile Map(ReferenceUserProfile profile)
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
            DeletedAt = profile.DeletedAt
        };
}
