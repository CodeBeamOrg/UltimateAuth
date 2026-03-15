using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class RoleProjection
{
    public RoleId Id { get; set; }

    public TenantKey Tenant { get; set; }

    public string Name { get; set; } = default!;

    public string NormalizedName { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }
}
