using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class CompiledPermissionSetTests
{
    [Fact]
    public void ExactPermission_Allows_Action()
    {
        var permissions = new[]
        {
            Permission.From("users.delete.admin")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.True(set.IsAllowed("users.delete.admin"));
    }

    [Fact]
    public void ExactPermission_DoesNotAllow_OtherAction()
    {
        var permissions = new[]
        {
            Permission.From("users.delete.admin")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.False(set.IsAllowed("users.create.admin"));
    }

    [Fact]
    public void WildcardPermission_AllowsEverything()
    {
        var permissions = new[]
        {
            Permission.Wildcard
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.True(set.IsAllowed("users.delete.admin"));
        Assert.True(set.IsAllowed("anything.really.admin"));
    }

    [Fact]
    public void PrefixPermission_AllowsChildren()
    {
        var permissions = new[]
        {
            Permission.From("users.*")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.True(set.IsAllowed("users.delete.admin"));
        Assert.True(set.IsAllowed("users.profile.update.admin"));
    }

    [Fact]
    public void PrefixPermission_DoesNotAllowOtherResource()
    {
        var permissions = new[]
        {
            Permission.From("users.*")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.False(set.IsAllowed("sessions.revoke.admin"));
    }

    [Fact]
    public void NestedPrefixPermission_Works()
    {
        var permissions = new[]
        {
            Permission.From("users.profile.*")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.True(set.IsAllowed("users.profile.update.admin"));
        Assert.True(set.IsAllowed("users.profile.get.self"));
    }

    [Fact]
    public void NestedPrefixPermission_DoesNotMatchParent()
    {
        var permissions = new[]
        {
            Permission.From("users.profile.*")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.False(set.IsAllowed("users.delete.admin"));
    }

    [Fact]
    public void PrefixPermission_DoesNotMatchSimilarPrefix()
    {
        var permissions = new[]
        {
            Permission.From("users.*")
        };

        var set = new CompiledPermissionSet(permissions);

        Assert.False(set.IsAllowed("usersettings.update.admin"));
    }

    [Fact]
    public void SetPermissions_ShouldReplaceExistingPermissions()
    {
        var now = DateTimeOffset.UtcNow;

        var permissions = UAuthPermissionCatalog.GetAdminPermissions();

        var oldPermission = Permission.From(permissions[0]);
        var newPermission = Permission.From(permissions[1]);

        var role = Role.Create(
            id: null,
            tenant: TenantKeys.Single,
            name: "test-role",
            permissions: [oldPermission],
            now: now);

        role.SetPermissions([newPermission], now.AddMinutes(1));

        role.Permissions.Should().Contain(newPermission);
        role.Permissions.Should().NotContain(oldPermission);
    }
}
