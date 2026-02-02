using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed record class UserLifecycle
{
    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; init; } = default!;

    public UserStatus Status { get; set; } = UserStatus.Active;
    public Guid SecurityStamp { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
