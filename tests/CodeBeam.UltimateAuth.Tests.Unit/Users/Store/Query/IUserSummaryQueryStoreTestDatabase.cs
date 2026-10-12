
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public interface IUserSummaryQueryStoreTestDatabase : IAsyncDisposable
{
    IUserSummaryQueryStore CreateStore(TenantKey tenant);
    Task SeedAsync(TenantKey tenant, IReadOnlyList<UserSummarySeed> users);
}

public sealed record UserSummarySeed
{
    public required UserKey UserKey { get; init; }

    public UserStatus Status { get; init; } = UserStatus.Active;

    public required DateTimeOffset CreatedAt { get; init; }

    public string? DisplayName { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }

    public ProfileKey ProfileKey { get; init; } = ProfileKey.Default;

    public bool IsDeleted { get; init; }
    public bool HasProfile { get; init; } = true;
}
