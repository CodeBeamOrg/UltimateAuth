using CodeBeam.UltimateAuth.Policies;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ActionTextTests
{
    [Theory]
    [InlineData("users.profile.get.admin", true)]
    [InlineData("users.profile.get.self", false)]
    [InlineData("users.profile.get", false)]
    public void RequireAdminPolicy_AppliesTo_Works(string action, bool expected)
    {
        var context = TestAccessContext.WithAction(action);
        var policy = new RequireAdminPolicy();
        Assert.Equal(expected, policy.AppliesTo(context));
    }

    [Fact]
    public void RequireAdminPolicy_DoesNotMatch_Substrings()
    {
        var context = TestAccessContext.WithAction("users.profile.get.administrator");
        var policy = new RequireAdminPolicy();
        Assert.False(policy.AppliesTo(context));
    }
}
