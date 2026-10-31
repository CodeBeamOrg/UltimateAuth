using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Services;

public sealed class RepairTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Repair_removes_only_own_stale_members_and_preserves_both_owners()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var store = new Mock<IUserSummaryQueryStore>();
        store.Setup(x => x.GetByKeysAsync(It.IsAny<IReadOnlyCollection<CodeBeam.UltimateAuth.Core.Domain.UserKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserSummary>());
        var stores = new Mock<IUserSummaryQueryStoreFactory>();
        stores.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(store.Object);
        var service = new SandboxRepairService(db, stores.Object, NullLogger<SandboxRepairService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RepairAsync(LabDatabase.MemberA));
        var result = await service.RepairAsync(LabDatabase.OwnerA);
        Assert.Equal(1, result.RemovedMemberships);
        await using var verify = db.CreateDbContext();
        var keys = await verify.SandboxMembers.Select(x => x.UserKey).ToListAsync();
        Assert.DoesNotContain(LabDatabase.MemberA, keys);
        Assert.Contains(LabDatabase.MemberB, keys);
        Assert.Contains(LabDatabase.OwnerA, keys);
        Assert.Contains(LabDatabase.OwnerB, keys);
    }
    [Fact]
    public async Task Preview_matches_repair_and_repeated_repair_preserves_valid_and_unrelated_members()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var validKey = CodeBeam.UltimateAuth.Core.Domain.UserKey.FromString("valid-member");
        await new SandboxService(db).AddMemberAsync(LabDatabase.OwnerA, validKey);
        var store = new Mock<IUserSummaryQueryStore>(MockBehavior.Strict);
        store.Setup(x => x.GetByKeysAsync(
                It.IsAny<IReadOnlyCollection<CodeBeam.UltimateAuth.Core.Domain.UserKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserSummary { UserKey = validKey } });
        var stores = new Mock<IUserSummaryQueryStoreFactory>();
        stores.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(store.Object);
        var service = new SandboxRepairService(db, stores.Object, NullLogger<SandboxRepairService>.Instance);
        var before = await db.SnapshotAsync();
        Assert.Equal(new SandboxRepairPreview(3, 1), await service.PreviewAsync(LabDatabase.OwnerA));
        Assert.Equal(before, await db.SnapshotAsync());
        Assert.Equal(1, (await service.RepairAsync(LabDatabase.OwnerA)).RemovedMemberships);
        var repaired = await db.SnapshotAsync();
        Assert.Equal(new SandboxRepairPreview(2, 0), await service.PreviewAsync(LabDatabase.OwnerA));
        Assert.Equal(0, (await service.RepairAsync(LabDatabase.OwnerA)).RemovedMemberships);
        Assert.Equal(repaired, await db.SnapshotAsync());
        await using var verify = db.CreateDbContext();
        var keys = await verify.SandboxMembers.Select(x => x.UserKey).ToListAsync();
        Assert.Equal(4, keys.Count);
        Assert.Contains(validKey, keys);
        Assert.Contains(LabDatabase.OwnerA, keys);
        Assert.Contains(LabDatabase.OwnerB, keys);
        Assert.Contains(LabDatabase.MemberB, keys);
        Assert.DoesNotContain(LabDatabase.MemberA, keys);
    }
}
