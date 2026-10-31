using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Services;

public sealed class DashboardTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(5, 5, 3)]
    [InlineData(100, SandboxLimits.AbsoluteMaxUsers, 18)]
    public async Task Overview_uses_own_membership_and_effective_capacity_when_optional_services_fail(
        int configuredLimit, int expectedLimit, int expectedRemaining)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        await using (var setup = db.CreateDbContext())
        {
            var settings = await setup.SandboxSettings.SingleAsync(x => x.Sandbox.OwnerUserKey == LabDatabase.OwnerA);
            settings.MaxUsers = configuredLimit;
            await setup.SaveChangesAsync();
        }
        var users = new Mock<IUserClient>(MockBehavior.Strict);
        users.Setup(x => x.GetMeAsync(null)).ThrowsAsync(new InvalidOperationException("Profile unavailable"));
        var sessions = new Mock<ISessionClient>(MockBehavior.Strict);
        sessions.Setup(x => x.GetMyChainsAsync(It.IsAny<CodeBeam.UltimateAuth.Core.Contracts.PageRequest>()))
            .ThrowsAsync(new InvalidOperationException("Sessions unavailable"));
        var authorization = new Mock<IAuthorizationClient>(MockBehavior.Strict);
        authorization.Setup(x => x.GetMyRolesAsync(It.IsAny<CodeBeam.UltimateAuth.Core.Contracts.PageRequest>()))
            .ThrowsAsync(new InvalidOperationException("Roles unavailable"));
        var client = new Mock<IUAuthClient>(MockBehavior.Strict);
        client.SetupGet(x => x.Users).Returns(users.Object);
        client.SetupGet(x => x.Sessions).Returns(sessions.Object);
        client.SetupGet(x => x.Authorization).Returns(authorization.Object);
        var service = new LabDashboardService(new(db), client.Object);

        var owner = await service.GetAsync(LabDatabase.OwnerA);
        Assert.NotNull(owner);
        Assert.Equal("Sandbox A", owner.SandboxName);
        Assert.True(owner.IsOwner);
        Assert.Equal(2, owner.MemberCount);
        Assert.Equal(expectedLimit, owner.MaxUsers);
        Assert.Equal(expectedRemaining, owner.RemainingCapacity);
        Assert.Null(owner.DisplayName);
        Assert.Null(owner.ChainCount);
        Assert.Null(owner.RoleCount);
        users.VerifyAll();
        sessions.VerifyAll();
        authorization.VerifyAll();

        var member = await service.GetAsync(LabDatabase.MemberA);
        Assert.NotNull(member);
        Assert.Equal(owner.SandboxId, member.SandboxId);
        Assert.False(member.IsOwner);
        Assert.Equal(owner.MemberCount, member.MemberCount);

        Assert.True(await new SandboxService(db).RemoveMemberAsync(LabDatabase.OwnerA, LabDatabase.MemberA));
        var after = await service.GetAsync(LabDatabase.OwnerA);
        Assert.NotNull(after);
        Assert.Equal(1, after.MemberCount);
        Assert.Equal(expectedLimit - 1, after.RemainingCapacity);
    }

    [Fact]
    public async Task Missing_membership_returns_no_dashboard_without_requesting_optional_data()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var client = new Mock<IUAuthClient>(MockBehavior.Strict);
        Assert.Null(await new LabDashboardService(new(db), client.Object).GetAsync(UserKey.FromString("orphan")));
        client.VerifyNoOtherCalls();
    }
}
