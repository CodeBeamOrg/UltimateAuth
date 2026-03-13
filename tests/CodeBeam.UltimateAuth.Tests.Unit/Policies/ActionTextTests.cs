using CodeBeam.UltimateAuth.Authorization.Policies;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ActionTextTests
{
    private readonly MustHavePermissionPolicy _policy = new();

    [Theory]
    [InlineData("users.profile.get.admin", true)]
    [InlineData("users.delete.admin", true)]
    [InlineData("sessions.revoke.admin", true)]
    public void AppliesTo_ReturnsTrue_ForAdminScope(string action, bool expected)
    {
        var context = TestAccessContext.WithAction(action);
        var result = _policy.AppliesTo(context);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("users.profile.get.self")]
    [InlineData("users.profile.get")]
    [InlineData("users.profile.get.anonymous")]
    public void AppliesTo_ReturnsFalse_ForNonAdminScope(string action)
    {
        var context = TestAccessContext.WithAction(action);
        var result = _policy.AppliesTo(context);
        Assert.False(result);
    }

    [Fact]
    public void AppliesTo_DoesNotMatch_Substrings()
    {
        var context = TestAccessContext.WithAction("users.profile.get.administrator");
        var result = _policy.AppliesTo(context);
        Assert.False(result);
    }

    [Fact]
    public void AppliesTo_IsCaseInsensitive()
    {
        var context = TestAccessContext.WithAction("users.profile.get.ADMIN");
        var result = _policy.AppliesTo(context);
        Assert.True(result);
    }

    [Theory]
    [InlineData("users.create.admin", true)]
    [InlineData("users.create.self", false)]
    [InlineData("users.create.anonymous", false)]
    [InlineData("users.create", false)]
    [InlineData("users.create.admin.extra", false)]
    public void AppliesTo_AdminScopeDetection(string action, bool expected)
    {
        var context = TestAccessContext.WithAction(action);
        var result = _policy.AppliesTo(context);
        Assert.Equal(expected, result);
    }
}
