using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Policy;
using CodeBeam.UltimateAuth.IdentityLab.Services;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Authorization;

public sealed class SandboxPolicyTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    public static TheoryData<string> TargetActions => new()
    {
        UAuthActions.UserProfiles.GetAdmin, UAuthActions.UserProfiles.UpdateAdmin,
        UAuthActions.Users.DeleteAdmin, UAuthActions.Sessions.GetChainAdmin,
        UAuthActions.Sessions.RevokeAllChainsAdmin, UAuthActions.Authorization.Roles.GetAdmin
    };

    [Theory]
    [MemberData(nameof(TargetActions))]
    public async Task Owner_cannot_access_foreign_or_missing_targets(string action)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), new());
        foreach (var target in new UserKey?[] { LabDatabase.MemberB, LabDatabase.OwnerB,
            UserKey.FromString("nonexistent"), null })
        {
            var denied = await policy.DecideAsync(Contexts.Create(action, LabDatabase.OwnerA, target));
            Assert.True(denied.IsDenied);
        }
        Assert.True((await policy.DecideAsync(Contexts.Create(action,
            LabDatabase.OwnerA, LabDatabase.MemberA))).IsAllowed);
        Assert.Equal(before, await db.SnapshotAsync());
    }

    [Theory]
    [InlineData(SandboxStatus.Passive, false)]
    [InlineData(SandboxStatus.Suspended, true)]
    public async Task Inactive_sandbox_blocks_writes_and_suspension_also_blocks_reads(SandboxStatus status, bool readDenied)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        await db.SetStatusAsync(status);
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), new());
        Assert.True((await policy.DecideAsync(Contexts.Create(UAuthActions.UserProfiles.UpdateAdmin,
            LabDatabase.OwnerA, LabDatabase.MemberA))).IsDenied);
        Assert.Equal(readDenied, (await policy.DecideAsync(Contexts.Create(UAuthActions.UserProfiles.GetAdmin,
            LabDatabase.OwnerA, LabDatabase.MemberA))).IsDenied);
    }

    [Fact]
    public async Task Members_cannot_manage_users_and_owners_cannot_be_deleted()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), new());
        Assert.Equal("sandbox_owner_required", (await policy.DecideAsync(Contexts.Create(
            UAuthActions.Users.DeleteAdmin, LabDatabase.MemberA, LabDatabase.OwnerA))).DenyReason);
        Assert.Equal("sandbox_owner_cannot_be_deleted", (await policy.DecideAsync(Contexts.Create(
            UAuthActions.Users.DeleteAdmin, LabDatabase.OwnerA, LabDatabase.OwnerA))).DenyReason);
        Assert.Equal("sandbox_owner_cannot_delete_self", (await policy.DecideAsync(Contexts.Create(
            UAuthActions.Users.DeleteSelf, LabDatabase.OwnerA, LabDatabase.OwnerA))).DenyReason);
    }

    [Fact]
    public async Task Batch_read_checks_every_target_and_requires_nonempty_targets()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), new());
        foreach (var targets in new[] { Array.Empty<UserKey>(), new[] { LabDatabase.MemberA, LabDatabase.MemberB } })
            Assert.True((await policy.DecideAsync(Contexts.Create(UAuthActions.Users.GetByKeysAdmin,
                LabDatabase.OwnerA, targets: targets))).IsDenied);
        Assert.True((await policy.DecideAsync(Contexts.Create(UAuthActions.Users.GetByKeysAdmin,
            LabDatabase.OwnerA, targets: new[] { LabDatabase.MemberA, LabDatabase.OwnerA }))).IsAllowed);
    }
}
