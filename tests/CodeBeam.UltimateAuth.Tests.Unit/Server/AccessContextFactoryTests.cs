using AwesomeAssertions;
using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class AccessContextFactoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldResolveTrustedActorRoles()
    {
        var tenant = TenantKey.Single;
        var user = UserKey.New();

        var ownerId = RoleId.New();
        var memberId = RoleId.New();
        var deletedId = RoleId.New();

        var userRoleStore = new Mock<IUserRoleStore>();
        var roleStore = new Mock<IRoleStore>();

        var userRoleFactory = new Mock<IUserRoleStoreFactory>();
        var roleFactory = new Mock<IRoleStoreFactory>();

        userRoleFactory
            .Setup(x => x.Create(tenant))
            .Returns(userRoleStore.Object);

        roleFactory
            .Setup(x => x.Create(tenant))
            .Returns(roleStore.Object);

        userRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new UserRole
                {
                    Tenant = tenant,
                    UserKey = user,
                    RoleId = ownerId
                },
                new UserRole
                {
                    Tenant = tenant,
                    UserKey = user,
                    RoleId = memberId
                },
                new UserRole
                {
                    Tenant = tenant,
                    UserKey = user,
                    RoleId = deletedId
                }
            });

        var now = DateTimeOffset.UtcNow;

        var owner = Role.Create(ownerId, tenant, "Owner", null, now);
        var member = Role.Create(memberId, tenant, "Member", null, now);
        var deleted = Role.Create(deletedId, tenant, "Deleted", null, now).MarkDeleted(now);

        roleStore
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<RoleId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { owner, member, deleted });

        var resolver = new AccessActorRoleResolver(
            userRoleFactory.Object,
            roleFactory.Object);

        var sut = new AccessContextFactory(
            resolver,
            Mock.Of<IUserIdConverterResolver>());

        var flow = AuthFlowTestFactory.New(
            tenant: tenant,
            userKey: user);

        var context = await sut.CreateAsync(
            flow,
            "users.get.self",
            "users");

        // Only active roles are exposed.
        context.ActorRoles.Should().HaveCount(2);

        context.ActorRoles.Should().ContainSingle(
            x => x.Id == ownerId.ToString() &&
                 x.Name == "Owner");

        context.ActorRoles.Should().ContainSingle(
            x => x.Id == memberId.ToString() &&
                 x.Name == "Member");

        context.ActorRoles.Should().NotContain(
            x => x.Id == deletedId.ToString());

        // Existing attributes contract remains available.
        context.Attributes.Should().ContainKey("roles");

        var attributeRoles =
            (RoleId[])context.Attributes["roles"];

        attributeRoles.Should().BeEquivalentTo(new[] { ownerId, memberId });
        attributeRoles.Should().NotContain(deletedId);

        // Both stores must resolve against the actor tenant.
        userRoleFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        roleFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        roleStore.Verify(
            x => x.GetByIdsAsync(
                It.Is<IReadOnlyCollection<RoleId>>(
                    ids => ids.Count == 3 &&
                           ids.Contains(ownerId) &&
                           ids.Contains(memberId) &&
                           ids.Contains(deletedId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldIgnoreUntrustedRolesForAnonymousActor()
    {
        var userRoleFactory = new Mock<IUserRoleStoreFactory>();
        var roleFactory = new Mock<IRoleStoreFactory>();

        var resolver = new AccessActorRoleResolver(
            userRoleFactory.Object,
            roleFactory.Object);

        var sut = new AccessContextFactory(
            resolver,
            Mock.Of<IUserIdConverterResolver>());

        var flow = AuthFlowTestFactory.New(
            isAuthenticated: false);

        var attributes = new Dictionary<string, object>
        {
            ["roles"] = new[] { "Admin" },
            ["custom"] = "value"
        };

        var context = await sut.CreateAsync(
            flow,
            "users.get.anonymous",
            "users",
            attributes: attributes);

        // Anonymous actors must never receive trusted roles.
        context.ActorRoles.Should().BeEmpty();

        // Caller-supplied role metadata must be discarded.
        context.Attributes.Should().NotContainKey("roles");

        // Unrelated attributes remain available.
        context.Attributes["custom"].Should().Be("value");

        // No role stores should be accessed.
        userRoleFactory.VerifyNoOtherCalls();
        roleFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForUserTargetsAsync_ShouldNormalizeAndPreserveTargets()
    {
        var tenant = TenantKey.Single;
        var first = UserKey.New();
        var second = UserKey.New();

        var userRoleFactory = new Mock<IUserRoleStoreFactory>();
        var roleFactory = new Mock<IRoleStoreFactory>();

        var resolver = new AccessActorRoleResolver(
            userRoleFactory.Object,
            roleFactory.Object);

        var sut = new AccessContextFactory(
            resolver,
            Mock.Of<IUserIdConverterResolver>());

        var flow = AuthFlowTestFactory.New(
            tenant: tenant,
            isAuthenticated: false);

        var context = await sut.CreateForUserTargetsAsync(
            flow,
            "users.getbykeys.admin",
            "users",
            new[] { first, second, first });

        context.TargetUserKeys.Should().HaveCount(2);
        context.TargetUserKeys.Should().ContainInOrder(first, second);
        context.TargetUserKey.Should().BeNull();
        context.ResourceTenant.Should().Be(tenant);
        context.IsAdminContext.Should().BeTrue();
    }

    [Fact]
    public async Task CreateForUserTargetsAsync_ShouldSnapshotInputCollection()
    {
        var first = UserKey.New();
        var second = UserKey.New();

        var userRoleFactory = new Mock<IUserRoleStoreFactory>();
        var roleFactory = new Mock<IRoleStoreFactory>();

        var sut = new AccessContextFactory(
            new AccessActorRoleResolver(
                userRoleFactory.Object,
                roleFactory.Object),
            Mock.Of<IUserIdConverterResolver>());

        var flow = AuthFlowTestFactory.New(
            isAuthenticated: false);

        var input = new List<UserKey> { first };

        var context = await sut.CreateForUserTargetsAsync(
            flow,
            "users.getbykeys.admin",
            "users",
            input);

        input.Add(second);

        context.TargetUserKeys.Should().ContainSingle()
            .Which.Should().Be(first);
    }

    [Fact]
    public async Task CreateForUserTargetsAsync_WithAttribute_ShouldPreserveTargets()
    {
        var first = UserKey.New();
        var second = UserKey.New();

        var sut = new AccessContextFactory(
            new AccessActorRoleResolver(
                Mock.Of<IUserRoleStoreFactory>(),
                Mock.Of<IRoleStoreFactory>()),
            Mock.Of<IUserIdConverterResolver>());

        var flow = AuthFlowTestFactory.New(
            isAuthenticated: false);

        var context = await sut.CreateForUserTargetsAsync(
            flow,
            "users.getbykeys.admin",
            "users",
            new[] { first, second });

        var updated = context.WithAttribute("source", "test");

        updated.TargetUserKeys.Should()
            .BeEquivalentTo(new[] { first, second });

        updated.Attributes["source"].Should().Be("test");
        updated.TargetUserKey.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SingleTargetContext_ShouldHaveEmptyTargetUserKeys()
    {
        var sut = new AccessContextFactory(
            new AccessActorRoleResolver(
                Mock.Of<IUserRoleStoreFactory>(),
                Mock.Of<IRoleStoreFactory>()),
            Mock.Of<IUserIdConverterResolver>());

        var flow = AuthFlowTestFactory.New(
            isAuthenticated: false);

        var context = await sut.CreateAsync(
            flow,
            "users.query.admin",
            "users");

        context.TargetUserKeys.Should().BeEmpty();
    }
}
