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
}
