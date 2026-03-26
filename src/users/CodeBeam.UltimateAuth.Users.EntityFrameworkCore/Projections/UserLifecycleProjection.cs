using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

public sealed class UserLifecycleProjection
{
    public Guid Id { get; set; }

    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; set; } = default!;

    public UserStatus Status { get; set; }

    public long SecurityVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }
}
