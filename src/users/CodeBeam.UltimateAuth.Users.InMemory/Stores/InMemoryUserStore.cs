using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserStore : IUserStore<UserKey>
{
    private readonly InMemoryUserLifecycleStore _lifecycle;
    private readonly IUserProfileStore _profiles;

    public InMemoryUserStore(
        InMemoryUserLifecycleStore lifecycle,
        IUserProfileStore profiles)
    {
        _lifecycle = lifecycle;
        _profiles = profiles;
    }

    public async Task<AuthUserRecord<UserKey>?> FindByIdAsync(
        string? tenantId,
        UserKey userId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        throw new NotImplementedException();
        //var lifecycle = await _lifecycle.GetAsync(tenantId, userId, ct);
        //if (lifecycle is null || lifecycle.IsDeleted)
        //    return null;

        //var profile = await _profiles.GetAsync(tenantId, userId, ct);

        //return new AuthUserRecord<UserKey>
        //{
        //    Id = userId,
        //    Identifier =
        //        profile?.Email ??
        //        profile?.DisplayName ??
        //        userId.ToString(),

        //    IsActive = lifecycle.Status == UserStatus.Active,
        //    IsDeleted = lifecycle.IsDeleted,
        //    CreatedAt = lifecycle.CreatedAt,
        //    DeletedAt = lifecycle.DeletedAt
        //};
    }

    public async Task<AuthUserRecord<UserKey>?> FindByLoginAsync(
        string? tenantId,
        string login,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // InMemory limitation: scan profiles
        if (_profiles is not InMemoryUserProfileStore mem)
            throw new InvalidOperationException("InMemory only");

        var profile = mem.AllProfiles
            .FirstOrDefault(p =>
                !p.IsDeleted &&
                !string.IsNullOrWhiteSpace(p.Email) &&
                string.Equals(p.Email, login, StringComparison.OrdinalIgnoreCase));

        if (profile is null)
            return null;

        return await FindByIdAsync(tenantId, profile.UserKey, ct);
    }

    public async Task<bool> ExistsAsync(
        string? tenantId,
        UserKey userId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        throw new NotImplementedException();

        //var lifecycle = await _lifecycle.GetAsync(tenantId, userId, ct);
        //return lifecycle is not null && !lifecycle.IsDeleted;
    }
}

