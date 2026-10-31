using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Integration;

public sealed class UserWorkflowTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static async Task<UserKey> RegisterAsync(IServiceProvider sp, LabDatabase db, string name)
    {
        var registration = new LabRegistrationService(ActorProvider.For(null), sp.GetRequiredService<IUserApplicationService>(),
            sp.GetRequiredService<IRoleStoreFactory>(), sp.GetRequiredService<IUserRoleStoreFactory>(),
            new(db), new(db, NullLogger<ActivityLogService>.Instance));
        var result = await registration.RegisterAsync(new(name, $"{name}@example.test", "Owner-test-password-8!"));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        await using var verify = db.CreateDbContext();
        return (await verify.Sandboxes.SingleAsync(x => x.Name == $"{name}'s Sandbox")).OwnerUserKey;
    }

    private static LabUserProvisioningService Provisioning(IServiceProvider sp, LabDatabase db, UserKey owner) =>
        new(ActorProvider.For(owner), new(db), new(db), sp.GetRequiredService<IUserApplicationService>(),
            sp.GetRequiredService<IRoleStoreFactory>(), sp.GetRequiredService<IUserRoleStoreFactory>(),
            sp.GetRequiredService<LabProvisioningRollbackGate>(), NullLogger<LabUserProvisioningService>.Instance);

    private static SandboxUserService UserService(IServiceProvider sp, LabDatabase db, UserKey owner, IUAuthClient client) =>
        new(new(db), client, db, new(db), Provisioning(sp, db, owner),
            sp.GetRequiredService<IUserSummaryQueryStoreFactory>(), new(db, NullLogger<ActivityLogService>.Instance),
            sp.GetRequiredService<IRoleStoreFactory>(), sp.GetRequiredService<IUserRoleStoreFactory>(),
            NullLogger<SandboxUserService>.Instance);

    private static CreateUserRequest Request(string name) => new()
        { UserName = name, Email = $"{name}@example.test", Password = "Member-test-password-8!" };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Created_member_is_queryable_capacity_rejection_leaves_no_account_and_deletion_frees_capacity(bool selfDelete)
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var owner = await RegisterAsync(sp, db, "owner");
        var foreignOwner = await RegisterAsync(sp, db, "foreign");
        var foreign = await Provisioning(sp, db, foreignOwner).CreateMemberAsync(foreignOwner, Request("foreign-member"));
        Assert.True(foreign.IsSuccess, foreign.FailureReason);
        var foreignKey = UserKey.FromString(foreign.UserKey!);
        await using (var setup = db.CreateDbContext())
        {
            var settings = await setup.SandboxSettings.SingleAsync(x => x.Sandbox.OwnerUserKey == owner);
            settings.MaxUsers = 2;
            await setup.SaveChangesAsync();
        }
        var client = new Mock<IUAuthClient>(MockBehavior.Strict);
        var userClient = new Mock<IUserClient>(MockBehavior.Strict);
        client.SetupGet(x => x.Users).Returns(userClient.Object);
        var service = UserService(sp, db, owner, client.Object);
        await service.CreateAsync(owner, Request("member"));
        var visible = await service.GetUsersAsync(owner);
        Assert.NotNull(visible);
        var member = Assert.Single(visible.Users, x => !x.IsOwner).UserKey;
        Assert.Equal(2, visible.Users.Count);
        Assert.DoesNotContain(visible.Users, x => x.UserKey == foreignKey);
        var beforeRejected = await db.SnapshotAsync();
        // The facade reports failures; assert persistence rather than dialog wording.
        await service.CreateAsync(owner, Request("rejected-member"));
        Assert.Equal(beforeRejected, await db.SnapshotAsync());
        await using (var fresh = runtime.Services.CreateAsyncScope())
        {
            var summaries = fresh.ServiceProvider.GetRequiredService<IUserSummaryQueryStoreFactory>().Create(TenantKeys.Single);
            var all = await summaries.QueryAsync(new UserQuery { PageSize = 100, IncludeDeleted = true });
            Assert.DoesNotContain(all.Items, x => x.UserName == "rejected-member");
        }
        Assert.False((await service.DeleteAsync(owner, foreignKey)).Success);
        Assert.Equal(beforeRejected, await db.SnapshotAsync());
        userClient.VerifyNoOtherCalls();
        var application = sp.GetRequiredService<IUserApplicationService>();
        if (selfDelete)
        {
            userClient.Setup(x => x.DeleteMeAsync()).Returns(async () =>
            {
                await application.DeleteMeAsync(Contexts.Create("users.delete.self", member, member));
                return new UAuthResult { IsSuccess = true };
            });
            Assert.True(await new SandboxAccountDeletionService(db, client.Object,
                NullLogger<SandboxAccountDeletionService>.Instance).DeleteMemberAsync(member));
            userClient.Verify(x => x.DeleteMeAsync(), Times.Once);
        }
        else
        {
            userClient.Setup(x => x.DeleteUserAsync(member, It.IsAny<DeleteUserRequest>())).Returns(async (UserKey target, DeleteUserRequest request) =>
            {
                Assert.Equal(DeleteMode.Hard, request.Mode);
                var result = await application.DeleteUserAsync(Contexts.Create("users.delete.admin", owner, target), request);
                return new UAuthResult<UserDeleteResult> { IsSuccess = result.IsSuccess, Value = result };
            });
            Assert.True((await service.DeleteAsync(owner, member)).Success);
            userClient.Verify(x => x.DeleteUserAsync(member, It.IsAny<DeleteUserRequest>()), Times.Once);
        }
        await using var verification = runtime.Services.CreateAsyncScope();
        var lifecycleStore = verification.ServiceProvider.GetRequiredService<IUserLifecycleStoreFactory>().Create(TenantKeys.Single);
        var lifecycle = await lifecycleStore.GetAsync(new UserLifecycleKey(TenantKeys.Single, member));
        if (selfDelete) { Assert.NotNull(lifecycle); Assert.True(lifecycle.IsDeleted); }
        else Assert.Null(lifecycle);
        Assert.NotNull(await lifecycleStore.GetAsync(new UserLifecycleKey(TenantKeys.Single, foreignKey)));
        var remaining = await service.GetUsersAsync(owner);
        Assert.NotNull(remaining);
        Assert.Equal(owner, Assert.Single(remaining.Users).UserKey);
        // Optional dashboard calls may fail; its persisted count must still be usable.
        var dashboard = await new LabDashboardService(new(db), client.Object).GetAsync(owner);
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.MemberCount);
        Assert.Equal(1, dashboard.RemainingCapacity);
        await service.CreateAsync(owner, Request("replacement"));
        Assert.Equal(2, (await service.GetUsersAsync(owner))!.Users.Count);
        await using var final = db.CreateDbContext();
        Assert.False(await final.SandboxMembers.AnyAsync(x => x.UserKey == member));
        Assert.Equal(2, await final.SandboxMembers.CountAsync(x => x.Sandbox.OwnerUserKey == foreignOwner));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Owner_and_admin_recovery_restore_missing_membership_without_duplicate_or_foreign_changes(bool adminRepair)
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var owner = await RegisterAsync(sp, db, "repair-owner");
        var other = await RegisterAsync(sp, db, "other-owner");
        var member = await Provisioning(sp, db, owner).CreateMemberAsync(owner, Request("retained-member"));
        Assert.True(member.IsSuccess, member.FailureReason);
        var roles = sp.GetRequiredService<IRoleStoreFactory>();
        var assignments = sp.GetRequiredService<IUserRoleStoreFactory>();
        await using (var setup = db.CreateDbContext())
        {
            setup.SandboxMembers.Remove(await setup.SandboxMembers.SingleAsync(x => x.UserKey == owner));
            await setup.SaveChangesAsync();
        }
        if (adminRepair)
        {
            var context = Contexts.Create("identitylab.repair", LabAdminIdentity.UserKey);
            var service = new LabRepairService(db, sp.GetRequiredService<IUserSummaryQueryStoreFactory>(),
                roles, assignments, new LabAdminGuard(roles, assignments), NullLogger<LabRepairService>.Instance);
            var snapshot = await service.GetSnapshotAsync(context);
            Assert.True(snapshot.UserScanComplete);
            Assert.Contains(snapshot.OrphanUsers, x => x.UserKey == owner && x.OwnedSandboxId != null);
            Assert.DoesNotContain(snapshot.OrphanUsers, x => x.UserKey == other);
            Assert.True((await service.RepairOwnerAsync(context, owner)).Success);
            var repaired = await db.SnapshotAsync();
            Assert.False((await service.RepairOwnerAsync(context, owner)).Success);
            Assert.Equal(repaired, await db.SnapshotAsync());
            Assert.DoesNotContain((await service.GetSnapshotAsync(context)).OrphanUsers, x => x.UserKey == owner);
        }
        else
        {
            var context = Contexts.Create("identitylab.repair", owner);
            var service = new LabOwnerRepairService(db, sp.GetRequiredService<IUserLifecycleStoreFactory>(),
                roles, assignments, NullLogger<LabOwnerRepairService>.Instance);
            Assert.Equal(OwnerRepairState.CanRepair, (await service.GetStatusAsync(context)).State);
            Assert.True((await service.RepairAsync(context)).Success);
            var repaired = await db.SnapshotAsync();
            Assert.Equal(OwnerRepairState.NotRequired, (await service.GetStatusAsync(context)).State);
            Assert.False((await service.RepairAsync(context)).Success);
            Assert.Equal(repaired, await db.SnapshotAsync());
        }
        await using var final = db.CreateDbContext();
        var members = await final.SandboxMembers.ToListAsync();
        Assert.Equal(3, members.Count);
        var restored = Assert.Single(members, x => x.UserKey == owner);
        Assert.Equal(CodeBeam.UltimateAuth.IdentityLab.Domain.SandboxMemberRole.Owner, restored.Role);
        Assert.Contains(members, x => x.UserKey.Value == member.UserKey && x.SandboxId == restored.SandboxId);
        Assert.Contains(members, x => x.UserKey == other && x.SandboxId != restored.SandboxId);
        Assert.Equal(2, await final.Sandboxes.CountAsync());
    }
}
