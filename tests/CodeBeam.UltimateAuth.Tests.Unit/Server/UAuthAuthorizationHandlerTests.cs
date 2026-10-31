using AwesomeAssertions;
using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Authorization;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Authorization;

public sealed class UAuthAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenOrchestratorAllows_ShouldSucceedRequirement()
    {
        var orchestrator = new Mock<IAccessOrchestrator>();

        var http = CreateHttpContext(false);

        var handler = new UAuthAuthorizationHandler(
            orchestrator.Object,
            new HttpContextAccessor
            {
                HttpContext = http
            });

        var requirement =
            new UAuthActionRequirement("users.read");

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user: http.User,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();

        orchestrator.Verify(
            x => x.ExecuteAsync(
                It.Is<AccessContext>(c =>
                    c.Action == "users.read" &&
                    c.Resource == "users"),
                It.IsAny<IAccessCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenOrchestratorDenies_ShouldNotSucceedRequirement()
    {
        var orchestrator = new Mock<IAccessOrchestrator>();

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<IAccessCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UAuthAuthorizationException("denied"));

        var http = CreateHttpContext(false);

        var handler = new UAuthAuthorizationHandler(
            orchestrator.Object,
            new HttpContextAccessor
            {
                HttpContext = http
            });

        var requirement =
            new UAuthActionRequirement("users.delete");

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            http.User,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldPreserveFullAction()
    {
        var orchestrator = new Mock<IAccessOrchestrator>();

        AccessContext? captured = null;

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<IAccessCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<AccessContext, IAccessCommand, CancellationToken>(
                (ctx, _, _) => captured = ctx)
            .Returns(Task.CompletedTask);

        var http = CreateHttpContext(false);

        var handler = new UAuthAuthorizationHandler(
            orchestrator.Object,
            new HttpContextAccessor
            {
                HttpContext = http
            });

        var requirement =
            new UAuthActionRequirement(
                "users.security.sessions.revoke");

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            http.User,
            resource: null);

        await handler.HandleAsync(context);

        captured.Should().NotBeNull();
        captured!.Action.Should()
            .Be("users.security.sessions.revoke");

        captured.Resource.Should().Be("users");
    }

    [Fact]
    public async Task HandleAsync_WithAuthenticatedUser_ShouldPassActorToOrchestrator()
    {
        var orchestrator = new Mock<IAccessOrchestrator>();

        AccessContext? captured = null;

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<IAccessCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<AccessContext, IAccessCommand, CancellationToken>(
                (ctx, _, _) => captured = ctx)
            .Returns(Task.CompletedTask);

        var userKey = UserKey.New();
        var tenant = TenantKey.FromExternal("tenant-a");

        var http = CreateHttpContext(
            authenticated: true,
            userKey: userKey,
            tenant: tenant);

        var handler = new UAuthAuthorizationHandler(
            orchestrator.Object,
            new HttpContextAccessor
            {
                HttpContext = http
            });

        var requirement =
            new UAuthActionRequirement("users.read");

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            http.User,
            resource: null);

        await handler.HandleAsync(context);

        captured.Should().NotBeNull();

        captured!.IsAuthenticated.Should().BeTrue();
        captured.ActorUserKey.Should().Be(userKey);
        captured.ActorTenant.Should().Be(tenant);
        captured.ResourceTenant.Should().Be(tenant);

        context.HasSucceeded.Should().BeTrue();
    }

    private static DefaultHttpContext CreateHttpContext(bool authenticated, UserKey? userKey = null, TenantKey? tenant = null)
    {
        var currentUser = new Mock<ICurrentUser>();

        currentUser
            .SetupGet(x => x.IsAuthenticated)
            .Returns(authenticated);

        if (authenticated)
        {
            if (userKey is null)
                throw new ArgumentNullException(nameof(userKey));

            currentUser
                .SetupGet(x => x.UserKey)
                .Returns(userKey.Value);
        }

        var userRoleStore = new Mock<IUserRoleStore>();
        var roleStore = new Mock<IRoleStore>();

        var userRoleFactory = new Mock<IUserRoleStoreFactory>();
        var roleFactory = new Mock<IRoleStoreFactory>();

        userRoleFactory
            .Setup(x => x.Create(It.IsAny<TenantKey>()))
            .Returns(userRoleStore.Object);

        roleFactory
            .Setup(x => x.Create(It.IsAny<TenantKey>()))
            .Returns(roleStore.Object);

        userRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());

        var services = new ServiceCollection();

        services.AddSingleton(currentUser.Object);

        // Normal UltimateAuth Server runtime
        services.AddSingleton<IUAuthRuntimeMarker, ServerRuntimeMarker>();

        // Trusted actor role resolution
        services.AddSingleton(userRoleFactory.Object);
        services.AddSingleton(roleFactory.Object);
        services.AddScoped<AccessActorRoleResolver>();

        // Build provider after all registrations
        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext
        {
            RequestServices = provider
        };

        var resolvedTenant =
            tenant ?? TenantKey.FromExternal("test-tenant");

        http.Items[UAuthConstants.HttpItems.TenantContextKey] =
            UAuthTenantContext.Resolved(resolvedTenant);

        return http;
    }
}
