using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Policy;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Provisioning;

public sealed class RegistrationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Registration_propagates_trusted_context_and_persists_matching_owner_relationships()
    {
        await using var db = await postgres.CreateAsync();
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), new());
        var users = new Mock<IUserApplicationService>(MockBehavior.Strict);
        users.Setup(x => x.CreateUserAsync(It.IsAny<AccessContext>(), It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (AccessContext context, CreateUserRequest request, CancellationToken ct) =>
            {
                Assert.True((await policy.DecideAsync(context, ct)).IsAllowed);
                Assert.Equal("alice", request.UserName);
                return UserCreateResult.Success(LabDatabase.OwnerA);
            });
        var roleStore = new Mock<IRoleStore>();
        roleStore.Setup(x => x.GetByNameAsync("OWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Role.Create(LabRoleIds.Owner, TenantKeys.Single, "Owner", new HashSet<Permission>(), DateTimeOffset.UnixEpoch));
        var roles = new Mock<IRoleStoreFactory>();
        roles.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(roleStore.Object);
        var userStore = new Mock<IUserRoleStore>(MockBehavior.Strict);
        userStore.Setup(x => x.GetAssignmentsAsync(LabDatabase.OwnerA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());
        userStore.Setup(x => x.AssignAsync(LabDatabase.OwnerA, LabRoleIds.Owner,
            It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var assignments = new Mock<IUserRoleStoreFactory>();
        assignments.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(userStore.Object);
        var service = new LabRegistrationService(ActorProvider.For(null), users.Object, roles.Object,
            assignments.Object, new(db), new(db, NullLogger<ActivityLogService>.Instance));
        var result = await service.RegisterAsync(new LabRegistrationRequest(" alice ", "alice@example.test", "test-password"));
        Assert.True(result.IsSuccess);
        await using var verify = db.CreateDbContext();
        var sandbox = await verify.Sandboxes.Include(x => x.Members).SingleAsync();
        Assert.Equal(LabDatabase.OwnerA, sandbox.OwnerUserKey);
        Assert.Equal(LabDatabase.OwnerA, Assert.Single(sandbox.Members).UserKey);
        userStore.Verify(x => x.AssignAsync(LabDatabase.OwnerA, LabRoleIds.Owner,
            It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        userStore.Verify(x => x.GetAssignmentsAsync(LabDatabase.OwnerA, It.IsAny<CancellationToken>()), Times.Once);
        userStore.VerifyNoOtherCalls();
    }
}
