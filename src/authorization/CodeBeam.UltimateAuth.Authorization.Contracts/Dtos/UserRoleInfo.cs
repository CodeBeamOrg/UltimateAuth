using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public class UserRoleInfo
{
    public TenantKey Tenant { get; init; }
    public UserKey UserKey { get; init; }
    public RoleId RoleId { get; init; }
    public string Name { get; set; } = default!;
    public DateTimeOffset AssignedAt { get; init; }
}
