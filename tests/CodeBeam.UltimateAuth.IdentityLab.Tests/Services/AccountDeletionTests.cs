using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Services;

public sealed class AccountDeletionTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Membership_is_removed_only_after_framework_self_deletion_succeeds(bool success)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var users = new Mock<IUserClient>(MockBehavior.Strict);
        users.Setup(x => x.DeleteMeAsync()).ReturnsAsync(new UAuthResult { IsSuccess = success });
        var client = new Mock<IUAuthClient>(MockBehavior.Strict);
        client.SetupGet(x => x.Users).Returns(users.Object);
        var service = new SandboxAccountDeletionService(db, client.Object, NullLogger<SandboxAccountDeletionService>.Instance);
        Assert.Equal(success, await service.DeleteMemberAsync(LabDatabase.MemberA));
        await using var verify = db.CreateDbContext();
        Assert.Equal(!success, await verify.SandboxMembers.AnyAsync(x => x.UserKey == LabDatabase.MemberA));
        Assert.True(await verify.SandboxMembers.AnyAsync(x => x.UserKey == LabDatabase.MemberB));
        Assert.True(await verify.SandboxMembers.AnyAsync(x => x.UserKey == LabDatabase.OwnerA));
    }

    [Fact]
    public async Task Owner_self_deletion_never_calls_framework_client()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var client = new Mock<IUAuthClient>(MockBehavior.Strict);
        var service = new SandboxAccountDeletionService(db, client.Object, NullLogger<SandboxAccountDeletionService>.Instance);
        Assert.False(await service.DeleteMemberAsync(LabDatabase.OwnerA));
        client.VerifyNoOtherCalls();
        Assert.Equal(before, await db.SnapshotAsync());
    }
}
