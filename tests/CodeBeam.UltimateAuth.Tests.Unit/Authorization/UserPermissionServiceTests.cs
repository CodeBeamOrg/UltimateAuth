using AwesomeAssertions;
using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Reference;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UserPermissionServiceTests
{
    private const string Action =
        UAuthActions.Authorization.Permissions.GetSelf;

    [Fact]
    public async Task GetMyPermissionsAsync_ReturnsDistinctSortedPermissions()
    {
        var f = new Fixture();

        f.Permissions
            .Setup(x => x.GetPermissionsAsync(
                TenantKey.Single,
                f.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Permission.From("users.read"),
                Permission.From("reports.*"),
                Permission.From("users.read"),
                Permission.From("*"),
                Permission.From("orders.update")
            ]);

        var result = await f.Sut.GetMyPermissionsAsync(f.Context);

        result.Permissions.Select(x => x.Value)
            .Should().Equal(
                "*",
                "orders.update",
                "reports.*",
                "users.read");

        f.Orchestrator.Verify(x => x.ExecuteAsync(
            f.Context,
            It.IsAny<IAccessCommand<UserPermissionsResult>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMyPermissionsAsync_WhenNoPermissions_ReturnsEmpty()
    {
        var f = new Fixture();

        f.Permissions
            .Setup(x => x.GetPermissionsAsync(
                TenantKey.Single,
                f.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Permission>());

        var result = await f.Sut.GetMyPermissionsAsync(f.Context);

        result.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyPermissionsAsync_UsesResourceTenantAndActor()
    {
        var f = new Fixture();

        f.Permissions
            .Setup(x => x.GetPermissionsAsync(
                f.Context.ResourceTenant,
                f.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Permission>());

        await f.Sut.GetMyPermissionsAsync(f.Context);

        f.Permissions.Verify(x => x.GetPermissionsAsync(
            f.Context.ResourceTenant,
            f.UserKey,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMyPermissionsAsync_WhenTargetDiffers_Throws()
    {
        var f = new Fixture();

        var context = TestAccessContext.ForTargetUser(
            f.UserKey,
            UserKey.New(),
            Action,
            resource: "authorization.permissions");

        var act = () => f.Sut.GetMyPermissionsAsync(context);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        f.Permissions.VerifyNoOtherCalls();
        f.Orchestrator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyPermissionsAsync_WhenActionDiffers_Throws()
    {
        var f = new Fixture();

        var context = TestAccessContext.ForUser(
            f.UserKey,
            UAuthActions.Authorization.Roles.GetAdmin,
            resource: "authorization.permissions");

        var act = () => f.Sut.GetMyPermissionsAsync(context);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        f.Permissions.VerifyNoOtherCalls();
        f.Orchestrator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyPermissionsAsync_WhenUnauthenticated_Throws()
    {
        var f = new Fixture();

        var context = TestAccessContext.WithAction(Action);

        var act = () => f.Sut.GetMyPermissionsAsync(context);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        f.Permissions.VerifyNoOtherCalls();
        f.Orchestrator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyPermissionsAsync_WhenAuthorizationDenied_DoesNotReadPermissions()
    {
        var f = new Fixture(allowExecution: false);

        var act = () => f.Sut.GetMyPermissionsAsync(f.Context);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        f.Permissions.VerifyNoOtherCalls();
    }

    private sealed class Fixture
    {
        public UserKey UserKey { get; } = UserKey.New();

        public Mock<IUserPermissionStore> Permissions { get; }
            = new(MockBehavior.Strict);

        public Mock<IAccessOrchestrator> Orchestrator { get; }
            = new(MockBehavior.Strict);

        public AccessContext Context { get; }

        public UserPermissionService Sut { get; }

        public Fixture(bool allowExecution = true)
        {
            Context = TestAccessContext.ForUser(
                UserKey,
                Action,
                resource: "authorization.permissions");

            Orchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<IAccessCommand<UserPermissionsResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext,
                    IAccessCommand<UserPermissionsResult>,
                    CancellationToken>(
                    async (_, command, ct) =>
                    {
                        if (!allowExecution)
                        {
                            throw new UAuthAuthorizationException(
                                "access_denied");
                        }

                        return await command.ExecuteAsync(ct);
                    });

            Sut = new UserPermissionService(
                Orchestrator.Object,
                Permissions.Object);
        }
    }
}
