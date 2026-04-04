using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record AssignRoleRequest
{
    public required UserKey UserKey { get; init; }
    public required string RoleName { get; init; }
}
