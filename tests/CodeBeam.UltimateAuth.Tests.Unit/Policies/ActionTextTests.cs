using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies;

namespace CodeBeam.UltimateAuth.Tests.Unit.Policies
{
    public class ActionTextTests
    {
        [Theory]
        [InlineData("users.profile.get.admin", true)]
        [InlineData("users.profile.get.self", false)]
        [InlineData("users.profile.get", false)]
        public void RequireAdminPolicy_AppliesTo_Works(string action, bool expected)
        {
            var context = new AccessContext { Action = action };
            var policy = new RequireAdminPolicy();

            Assert.Equal(expected, policy.AppliesTo(context));
        }

        [Fact]
        public void RequireAdminPolicy_DoesNotMatch_Substrings()
        {
            var context = new AccessContext
            {
                Action = "users.profile.get.administrator"
            };

            var policy = new RequireAdminPolicy();

            Assert.False(policy.AppliesTo(context));
        }

    }
}
