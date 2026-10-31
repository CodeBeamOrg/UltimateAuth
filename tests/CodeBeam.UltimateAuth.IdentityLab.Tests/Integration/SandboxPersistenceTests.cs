using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Integration;

public sealed class SandboxPersistenceTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Creation_persists_owner_membership_and_settings_together()
    {
        await using var db = await postgres.CreateAsync();
        var sandbox = await new SandboxService(db).CreateAsync(LabDatabase.OwnerA, "  Sandbox A  ");
        await using var verify = db.CreateDbContext();
        var actual = await verify.Sandboxes.Include(x => x.Members).Include(x => x.Settings).SingleAsync();
        Assert.Equal(sandbox.Id, actual.Id);
        Assert.Equal("Sandbox A", actual.Name);
        Assert.Equal(LabDatabase.OwnerA, actual.OwnerUserKey);
        var owner = Assert.Single(actual.Members);
        Assert.Equal(LabDatabase.OwnerA, owner.UserKey);
        Assert.Equal(SandboxMemberRole.Owner, owner.Role);
        Assert.Equal(5, actual.Settings.MaxUsers);
    }

    [Fact]
    public async Task Cross_sandbox_member_removal_and_owner_removal_leave_database_unchanged()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var service = new SandboxService(db);
        Assert.False(await service.RemoveMemberAsync(LabDatabase.OwnerA, LabDatabase.MemberB));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveMemberAsync(LabDatabase.OwnerA, LabDatabase.OwnerA));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RemoveMemberAsync(LabDatabase.MemberA, LabDatabase.MemberA));
        Assert.Equal(before, await db.SnapshotAsync());
        Assert.True(await service.RemoveMemberAsync(LabDatabase.OwnerA, LabDatabase.MemberA));
        await using var verify = db.CreateDbContext();
        Assert.False(await verify.SandboxMembers.AnyAsync(x => x.UserKey == LabDatabase.MemberA));
        Assert.True(await verify.SandboxMembers.AnyAsync(x => x.UserKey == LabDatabase.MemberB));
    }

    [Fact]
    public async Task Duplicate_owner_creation_rolls_back_all_new_sandbox_state()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => new SandboxService(db).CreateAsync(LabDatabase.OwnerA, "Duplicate"));
        Assert.Equal(before, await db.SnapshotAsync());
    }

    [Fact]
    public async Task Concurrent_member_additions_cannot_exceed_capacity()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        await using (var setup = db.CreateDbContext())
        {
            var settings = await setup.SandboxSettings.SingleAsync(x => x.Sandbox.OwnerUserKey == LabDatabase.OwnerA);
            settings.MaxUsers = 3;
            await setup.SaveChangesAsync();
        }
        var service = new SandboxService(db);
        async Task<Exception?> Add(string key) => await Record.ExceptionAsync(async () =>
            { await service.AddMemberAsync(LabDatabase.OwnerA, UserKey.FromString(key)); });
        // No sleeps: serializable transactions must preserve the invariant for any scheduling.
        var outcomes = await Task.WhenAll(Add("candidate-1"), Add("candidate-2"));
        Assert.Single(outcomes, x => x is null);
        await using var verify = db.CreateDbContext();
        Assert.Equal(3, await verify.SandboxMembers.CountAsync(x => x.Sandbox.OwnerUserKey == LabDatabase.OwnerA));
        Assert.Equal(2, await verify.SandboxMembers.CountAsync(x => x.Sandbox.OwnerUserKey == LabDatabase.OwnerB));
    }

    [Fact]
    public async Task Member_cannot_be_added_to_second_sandbox()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SandboxService(db).AddMemberAsync(LabDatabase.OwnerA, LabDatabase.MemberB));
        Assert.Equal(before, await db.SnapshotAsync());
    }
    [Fact]
    public async Task Concurrent_creation_cannot_create_two_sandboxes_for_one_owner()
    {
        await using var db = await postgres.CreateAsync();
        var service = new SandboxService(db);
        async Task<Exception?> Create(string name) => await Record.ExceptionAsync(async () =>
            { await service.CreateAsync(LabDatabase.OwnerA, name); });
        var outcomes = await Task.WhenAll(Create("First"), Create("Second"));
        Assert.Single(outcomes, x => x is null);
        await using var verify = db.CreateDbContext();
        Assert.Equal(1, await verify.Sandboxes.CountAsync());
        Assert.Equal(1, await verify.SandboxMembers.CountAsync());
        Assert.Equal(1, await verify.SandboxSettings.CountAsync());
    }

}
