using AwesomeAssertions;
using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Authorization;
using CodeBeam.UltimateAuth.Server.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Authorization;

public sealed class ResourceAccessContextBuilderTests
{
    [Fact]
    public async Task Create_WithAuthenticatedUser_ShouldSetActor()
    {
        var userKey = CreateUserKey();

        var http = CreateHttpContext(authenticated: true, userKey);

        var result = await ResourceAccessContextBuilder.CreateAsync(http, "users.read");

        result.IsAuthenticated.Should().BeTrue();
        result.ActorUserKey.Should().Be(userKey);
        result.Action.Should().Be("users.read");
        result.Resource.Should().Be("users");
    }

    [Fact]
    public async Task Create_WithAnonymousUser_ShouldNotSetActorUserKey()
    {
        var http = CreateHttpContext(authenticated: false);

        var result = await ResourceAccessContextBuilder.CreateAsync(http, "users.read");

        result.IsAuthenticated.Should().BeFalse();
        result.ActorUserKey.Should().BeNull();
    }

    [Theory]
    [InlineData("users.read", "users")]
    [InlineData("users.delete", "users")]
    [InlineData("users.security.sessions.revoke", "users")]
    [InlineData("roles.assign", "roles")]
    [InlineData("sessions.revoke", "sessions")]
    public async Task Create_ShouldDeriveResourceFromAction(string action, string expectedResource)
    {
        var http = CreateHttpContext(authenticated: false);

        var result = await ResourceAccessContextBuilder.CreateAsync(http, action);

        result.Resource.Should().Be(expectedResource);
        result.Action.Should().Be(action);
    }

    [Fact]
    public async Task Create_ShouldNotTreatCurrentUserAsSystemActor()
    {
        var http = CreateHttpContext(authenticated: true, CreateUserKey());

        var result = await ResourceAccessContextBuilder.CreateAsync(http, "users.read");

        result.IsSystemActor.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldNotSetTargetUser()
    {
        var http = CreateHttpContext(authenticated: true, CreateUserKey());

        var result = await ResourceAccessContextBuilder.CreateAsync(http, "users.read");

        result.TargetUserKey.Should().BeNull();
    }

    [Fact]
    public async Task Create_ShouldPropagateTenantToActorAndResource()
    {
        var tenant = TenantKey.FromExternal("tenant-a");

        var http = CreateHttpContext(authenticated: true, userKey: CreateUserKey(), tenant: tenant);

        var result = await ResourceAccessContextBuilder.CreateAsync(http, "users.read");

        result.ActorTenant.Should().Be(tenant);
        result.ResourceTenant.Should().Be(tenant);
    }

    [Fact]
    public async Task Create_WithoutTenantContext_ShouldThrow()
    {
        var currentUser = new Mock<ICurrentUser>();

        currentUser
            .SetupGet(x => x.IsAuthenticated)
            .Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(currentUser.Object);

        var http = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        Func<Task> act = () =>
            ResourceAccessContextBuilder.CreateAsync(
                http,
                "users.read");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*TenantContext is missing*");
    }

    private static DefaultHttpContext CreateHttpContext(bool authenticated, UserKey? userKey = null, TenantKey? tenant = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(authenticated);

        if (authenticated)
        {
            if (userKey is null)
                throw new ArgumentNullException(nameof(userKey));

            currentUser.SetupGet(x => x.UserKey).Returns(userKey.Value);
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

        // Register all dependencies before building the provider.
        var services = new ServiceCollection();

        services.AddSingleton(currentUser.Object);
        services.AddSingleton<IUAuthRuntimeMarker, ServerRuntimeMarker>();

        services.AddSingleton(userRoleFactory.Object);
        services.AddSingleton(roleFactory.Object);

        services.AddScoped<AccessActorRoleResolver>();

        // Build the provider only after completing registrations.
        var http = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var resolvedTenant =
            tenant ?? TenantKey.FromExternal("test-tenant");

        http.Items[UAuthConstants.HttpItems.TenantContextKey] =
            UAuthTenantContext.Resolved(resolvedTenant);

        return http;
    }

    private static UserKey CreateUserKey()
    {
        return UserKey.New();
    }
}
