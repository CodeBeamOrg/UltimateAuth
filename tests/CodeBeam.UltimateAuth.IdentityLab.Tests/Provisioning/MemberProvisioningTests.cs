using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Policy;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Provisioning;

public sealed class MemberProvisioningTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("owner-b")]
    [InlineData("member-a")]
    public async Task Forged_actor_or_regular_member_cannot_provision(string requestedActor)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var users = new Mock<IUserApplicationService>(MockBehavior.Strict);
        var roles = new Mock<IRoleStoreFactory>(MockBehavior.Strict);
        var assignments = new Mock<IUserRoleStoreFactory>(MockBehavior.Strict);
        var service = new LabUserProvisioningService(ActorProvider.For(LabDatabase.MemberA),
            new(db), new(db), users.Object, roles.Object, assignments.Object, new(),
            NullLogger<LabUserProvisioningService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateMemberAsync(
            UserKey.FromString(requestedActor), new CreateUserRequest()));
        Assert.Equal(before, await db.SnapshotAsync());
        users.VerifyNoOtherCalls();
        roles.VerifyNoOtherCalls();
        assignments.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Failed_role_assignment_rolls_back_only_created_user_and_token_cannot_be_replayed()
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var gate = new LabProvisioningRollbackGate();
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), gate);
        var createdKey = UserKey.FromString("new-member");
        var users = new Mock<IUserApplicationService>(MockBehavior.Strict);
        users.Setup(x => x.CreateUserAsync(It.IsAny<AccessContext>(), It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (AccessContext context, CreateUserRequest request, CancellationToken ct) =>
            {
                Assert.True((await policy.DecideAsync(context, ct)).IsAllowed);
                return UserCreateResult.Success(createdKey);
            });
        AccessContext? rollbackContext = null;
        bool? unrelatedDenied = null, rollbackAllowed = null, replayDenied = null;
        DeleteMode? rollbackMode = null;
        users.Setup(x => x.DeleteUserAsync(It.IsAny<AccessContext>(), It.IsAny<DeleteUserRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (AccessContext context, DeleteUserRequest request, CancellationToken ct) =>
            {
                rollbackContext = context;
                rollbackMode = request.Mode;
                // An unrelated target cannot consume the real token.
                var unrelated = Contexts.Create(UAuthActions.Users.DeleteAdmin, LabDatabase.OwnerA,
                    UserKey.FromString("unrelated"), attributes: context.Attributes);
                unrelatedDenied = (await policy.DecideAsync(unrelated, ct)).IsDenied;
                rollbackAllowed = (await policy.DecideAsync(context, ct)).IsAllowed;
                replayDenied = (await policy.DecideAsync(context, ct)).IsDenied;
                return UserDeleteResult.Success(DeleteMode.Hard);
            });
        var roleStore = new Mock<IRoleStore>();
        roleStore.Setup(x => x.GetByNameAsync("USER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Role.Create(LabRoleIds.User, TenantKeys.Single, "User", new HashSet<Permission>(), DateTimeOffset.UnixEpoch));
        var roles = new Mock<IRoleStoreFactory>();
        roles.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(roleStore.Object);
        var userStore = new Mock<IUserRoleStore>(MockBehavior.Strict);
        userStore.Setup(x => x.GetAssignmentsAsync(createdKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Injected assignment failure"));
        var assignments = new Mock<IUserRoleStoreFactory>();
        assignments.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(userStore.Object);
        var service = new LabUserProvisioningService(ActorProvider.For(LabDatabase.OwnerA), new(db), new(db),
            users.Object, roles.Object, assignments.Object, gate, NullLogger<LabUserProvisioningService>.Instance);
        await Assert.ThrowsAsync<LabPartialProvisioningException>(() => service.CreateMemberAsync(
            LabDatabase.OwnerA, new CreateUserRequest()));
        Assert.NotNull(rollbackContext);
        Assert.Equal(createdKey, rollbackContext!.TargetUserKey);
        Assert.Equal(DeleteMode.Hard, rollbackMode);
        Assert.True(unrelatedDenied);
        Assert.True(rollbackAllowed);
        Assert.True(replayDenied);
        Assert.True((await policy.DecideAsync(rollbackContext!)).IsDenied);
        users.Verify(x => x.DeleteUserAsync(It.IsAny<AccessContext>(), It.IsAny<DeleteUserRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(before, await db.SnapshotAsync());
    }
}
