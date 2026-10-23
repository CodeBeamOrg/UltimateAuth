using CodeBeam.UltimateAuth.Authorization.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public static class LabRoleIds
{
    public static readonly RoleId Admin = RoleId.From(
        Guid.Parse("a91a7b31-1b50-4e91-8a71-98b2e6d0b001"));

    public static readonly RoleId Owner = RoleId.From(
        Guid.Parse("a91a7b31-1b50-4e91-8a71-98b2e6d0b002"));

    public static readonly RoleId User = RoleId.From(
        Guid.Parse("a91a7b31-1b50-4e91-8a71-98b2e6d0b003"));
}
