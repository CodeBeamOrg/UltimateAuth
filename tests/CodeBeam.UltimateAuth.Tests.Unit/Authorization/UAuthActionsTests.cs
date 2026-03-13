using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.Tests.Unit.Actions;

public class UAuthActionsTests
{
    [Fact]
    public void Create_WithResourceOperationScope_Works()
    {
        var action = UAuthActions.Create("users", "delete", ActionScope.Admin);
        Assert.Equal("users.delete.admin", action);
    }

    [Fact]
    public void Create_WithSubResource_Works()
    {
        var action = UAuthActions.Create("users", "update", ActionScope.Self, "profile");
        Assert.Equal("users.profile.update.self", action);
    }

    [Fact]
    public void Create_ProducesLowercaseScope()
    {
        var action = UAuthActions.Create("users", "delete", ActionScope.Admin);
        Assert.Equal("users.delete.admin", action);
    }

    [Fact]
    public void Create_DifferentScopes_Work()
    {
        var self = UAuthActions.Create("users", "update", ActionScope.Self);
        var admin = UAuthActions.Create("users", "delete", ActionScope.Admin);
        var anon = UAuthActions.Create("users", "create", ActionScope.Anonymous);

        Assert.Equal("users.update.self", self);
        Assert.Equal("users.delete.admin", admin);
        Assert.Equal("users.create.anonymous", anon);
    }

    [Fact]
    public void Create_DoesNotCreateDoubleDots_WhenSubResourceNull()
    {
        var action = UAuthActions.Create("users", "delete", ActionScope.Admin);
        Assert.DoesNotContain("..", action);
    }
}
