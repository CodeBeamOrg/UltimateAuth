using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Regression;

public sealed class SystemRoleIdentityTests
{
    [Fact]
    [Trait("KnownDefect", "SystemRoleIdentity")]
    public async Task Owner_name_with_admin_id_must_be_rejected_before_assignment()
    {
        // Security expectation: fail closed on a corrupt system-role definition.
        // Current LabRoleService checks the name but does not pin its ID.
        var store = new Mock<IRoleStore>();
        store.Setup(x => x.GetByNameAsync("OWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Role.Create(LabRoleIds.Admin, TenantKeys.Single, "Owner",
                new HashSet<Permission>(), DateTimeOffset.UnixEpoch));
        var roles = new Mock<IRoleStoreFactory>();
        roles.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(store.Object);
        var userStore = new Mock<IUserRoleStore>();
        userStore.Setup(x => x.GetAssignmentsAsync(LabDatabase.MemberA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());
        userStore.Setup(x => x.AssignAsync(LabDatabase.MemberA, LabRoleIds.Admin,
            It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var assignments = new Mock<IUserRoleStoreFactory>();
        assignments.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(userStore.Object);
        var service = new LabRoleService(roles.Object, assignments.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignAsync(
            TenantKeys.Single, LabDatabase.MemberA, "Owner"));
        userStore.Verify(x => x.AssignAsync(LabDatabase.MemberA, LabRoleIds.Admin,
            It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
