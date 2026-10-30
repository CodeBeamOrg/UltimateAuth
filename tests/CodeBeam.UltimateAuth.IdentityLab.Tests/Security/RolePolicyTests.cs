using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Services;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Security;

public sealed class RolePolicyTests
{
    [Theory]
    [InlineData("owner-a")]
    [InlineData("member-a")]
    [InlineData("ladmin")]
    public async Task Public_role_assignment_is_denied_even_when_context_claims_admin(string actor)
    {
        var context = Contexts.Create(UAuthActions.Authorization.Roles.AssignAdmin,
            CodeBeam.UltimateAuth.Core.Domain.UserKey.FromString(actor),
            LabDatabase.MemberA, attributes: new Dictionary<string, object> { ["roles"] = new[] { LabRoleIds.Admin } });
        var result = await new LabApplicationPolicy().DecideAsync(context);
        Assert.True(result.IsDenied);
        Assert.Equal("lab_role_management_restricted", result.DenyReason);
    }

    [Fact]
    public async Task Trusted_system_role_assignment_and_authenticated_read_remain_allowed()
    {
        var policy = new LabApplicationPolicy();
        Assert.True((await policy.DecideAsync(Contexts.Create(
            UAuthActions.Authorization.Roles.AssignAdmin, system: true))).IsAllowed);
        Assert.True((await policy.DecideAsync(Contexts.Create(
            UAuthActions.Authorization.Roles.GetSelf, LabDatabase.MemberA))).IsAllowed);
        Assert.True((await policy.DecideAsync(Contexts.Create(
            UAuthActions.Authorization.Roles.GetSelf))).IsDenied);
    }

    [Theory]
    [InlineData("Ladmin")]
    [InlineData("Owner")]
    [InlineData("Manager")]
    [InlineData("Customer")]
    public void Role_names_cannot_forge_root_identity(string name)
    {
        var context = Contexts.Create("users.delete.admin", LabDatabase.MemberA,
            attributes: new Dictionary<string, object> { ["roles"] = new[] { name } });
        Assert.False(new LabRootResolver().IsLadmin(context));
    }

    [Fact]
    public void Root_resolution_requires_authenticated_actor_and_typed_admin_id()
    {
        var attributes = new Dictionary<string, object> { ["roles"] = new[] { LabRoleIds.Admin } };
        var resolver = new LabRootResolver();
        Assert.True(resolver.IsLadmin(Contexts.Create("test", LabDatabase.OwnerA, attributes: attributes)));
        Assert.False(resolver.IsLadmin(Contexts.Create("test", attributes: attributes)));
        Assert.False(resolver.IsLadmin(Contexts.Create("test", LabDatabase.OwnerA, system: true, attributes: attributes)));
        Assert.False(resolver.IsLadmin(Contexts.Create("test", LabDatabase.OwnerA,
            attributes: new Dictionary<string, object> { ["roles"] = new[] { LabRoleIds.Manager, LabRoleIds.Customer } })));
    }
}
