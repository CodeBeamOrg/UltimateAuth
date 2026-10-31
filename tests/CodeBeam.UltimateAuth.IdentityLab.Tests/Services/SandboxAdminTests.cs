using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Services;

public sealed class SandboxAdminTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly AccessContext Admin = Contexts.Create("identitylab.tests", UserKey.FromString("admin"));

    private static SandboxAdminService Create(LabDatabase db)
    {
        // Guard authorization is covered separately; these tests exercise persisted admin workflows.
        var guard = new Mock<ILabAdminGuard>(MockBehavior.Strict);
        guard.Setup(x => x.EnsureAdminAsync(Admin, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return new(db, guard.Object);
    }

    [Fact]
    public async Task Capacity_changes_respect_existing_members_and_control_subsequent_additions()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var sandboxes = new SandboxService(db);
        var sandbox = (await sandboxes.GetByUserAsync(LabDatabase.OwnerA))!;
        var service = Create(db);
        var before = await db.SnapshotAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetMaxUsersAsync(Admin, sandbox.Id, 1));
        Assert.Equal(before, await db.SnapshotAsync());
        Assert.True(await service.SetMaxUsersAsync(Admin, sandbox.Id, 2));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sandboxes.AddMemberAsync(LabDatabase.OwnerA, UserKey.FromString("new-member")));
        Assert.True(await service.SetMaxUsersAsync(Admin, sandbox.Id, 3));
        await sandboxes.AddMemberAsync(LabDatabase.OwnerA, UserKey.FromString("new-member"));
        var summary = await service.GetAsync(Admin, sandbox.Id);
        Assert.NotNull(summary);
        Assert.Equal(3, summary.MemberCount);
        Assert.Equal(3, summary.MaxUsers);
        Assert.Equal(2, (await sandboxes.GetMembersAsync((await sandboxes.GetByUserAsync(LabDatabase.OwnerB))!.Id)).Count);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    public async Task Search_treats_postgres_pattern_characters_literally(string literal)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var sandbox = (await new SandboxService(db).GetByUserAsync(LabDatabase.OwnerA))!;
        var service = Create(db);
        Assert.True(await service.RenameAsync(Admin, sandbox.Id, $"  Lab {literal} special  "));
        var result = await service.QueryAsync(Admin, new SandboxQuery { Search = $" {literal} " });
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(sandbox.Id, Assert.Single(result.Items).Id);
        Assert.Equal($"Lab {literal} special", result.Items[0].Name);
    }

    [Fact]
    public async Task Statistics_follow_status_transitions_and_membership_removal()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var sandboxes = new SandboxService(db);
        var a = (await sandboxes.GetByUserAsync(LabDatabase.OwnerA))!;
        var b = (await sandboxes.GetByUserAsync(LabDatabase.OwnerB))!;
        var service = Create(db);
        Assert.Equal(new SandboxStatistics(2, 2, 0, 0, 4), await service.GetStatisticsAsync(Admin));
        Assert.True(await sandboxes.RemoveMemberAsync(LabDatabase.OwnerA, LabDatabase.MemberA));
        Assert.True(await service.SetStatusAsync(Admin, a.Id, SandboxStatus.Passive));
        Assert.True(await service.SetStatusAsync(Admin, b.Id, SandboxStatus.Suspended));
        Assert.Equal(new SandboxStatistics(2, 0, 1, 1, 3), await service.GetStatisticsAsync(Admin));
        var suspended = await service.QueryAsync(Admin, new SandboxQuery { Status = SandboxStatus.Suspended });
        Assert.Equal(1, suspended.TotalCount);
        Assert.Equal(b.Id, Assert.Single(suspended.Items).Id);
    }
}
