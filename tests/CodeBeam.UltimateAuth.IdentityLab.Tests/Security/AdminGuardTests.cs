using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Security;

public sealed class AdminGuardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Admin_guard_uses_store_assignment_even_when_attributes_claim_ladmin(bool assigned)
    {
        var store = new Mock<IRoleStore>();
        store.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<RoleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Role.Create(LabRoleIds.Admin, TenantKeys.Single, "Ladmin",
                new HashSet<Permission>(), DateTimeOffset.UnixEpoch) });
        var roles = new Mock<IRoleStoreFactory>();
        roles.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(store.Object);
        var userStore = new Mock<IUserRoleStore>();
        userStore.Setup(x => x.GetAssignmentsAsync(LabDatabase.OwnerA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigned ? new[] { new UserRole { RoleId = LabRoleIds.Admin } } : Array.Empty<UserRole>());
        var users = new Mock<IUserRoleStoreFactory>();
        users.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(userStore.Object);
        var context = Contexts.Create("identitylab.admin", LabDatabase.OwnerA,
            attributes: new Dictionary<string, object> { ["roles"] = new[] { LabRoleIds.Admin } });
        Assert.Equal(assigned, await new LabAdminGuard(roles.Object, users.Object).IsAdminAsync(context));
    }

    [Theory]
    [InlineData("Ladmin")]
    [InlineData("ladmin")]
    [InlineData("Manager")]
    [InlineData("Customer")]
    public async Task Provisioning_role_helper_rejects_roles_outside_owner_and_user(string name)
    {
        var roles = new Mock<IRoleStoreFactory>(MockBehavior.Strict);
        var users = new Mock<IUserRoleStoreFactory>(MockBehavior.Strict);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CodeBeam.UltimateAuth.IdentityLab.Services.LabRoleService(
            roles.Object, users.Object).AssignAsync(TenantKeys.Single, LabDatabase.MemberA, name));
        roles.VerifyNoOtherCalls();
        users.VerifyNoOtherCalls();
    }
}
