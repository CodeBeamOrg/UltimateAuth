using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Authorization;

public sealed class ResourceAccessContextBuilderTests
{
    [Fact]
    public void Create_WithAuthenticatedUser_ShouldSetActor()
    {
        var userKey = CreateUserKey();

        var http = CreateHttpContext(
            authenticated: true,
            userKey);

        var result =
            ResourceAccessContextBuilder.Create(
                http,
                "users.read");

        result.IsAuthenticated.Should().BeTrue();
        result.ActorUserKey.Should().Be(userKey);
        result.Action.Should().Be("users.read");
        result.Resource.Should().Be("users");
    }

    [Fact]
    public void Create_WithAnonymousUser_ShouldNotSetActorUserKey()
    {
        var http = CreateHttpContext(
            authenticated: false);

        var result =
            ResourceAccessContextBuilder.Create(
                http,
                "users.read");

        result.IsAuthenticated.Should().BeFalse();
        result.ActorUserKey.Should().BeNull();
    }

    [Theory]
    [InlineData("users.read", "users")]
    [InlineData("users.delete", "users")]
    [InlineData("users.security.sessions.revoke", "users")]
    [InlineData("roles.assign", "roles")]
    [InlineData("sessions.revoke", "sessions")]
    public void Create_ShouldDeriveResourceFromAction(
        string action,
        string expectedResource)
    {
        var http = CreateHttpContext(
            authenticated: false);

        var result =
            ResourceAccessContextBuilder.Create(
                http,
                action);

        result.Resource.Should().Be(expectedResource);
        result.Action.Should().Be(action);
    }

    [Fact]
    public void Create_ShouldNotTreatCurrentUserAsSystemActor()
    {
        var http = CreateHttpContext(
            authenticated: true,
            CreateUserKey());

        var result =
            ResourceAccessContextBuilder.Create(
                http,
                "users.read");

        result.IsSystemActor.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldNotSetTargetUser()
    {
        var http = CreateHttpContext(
            authenticated: true,
            CreateUserKey());

        var result =
            ResourceAccessContextBuilder.Create(
                http,
                "users.read");

        result.TargetUserKey.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldPropagateTenantToActorAndResource()
    {
        var tenant = TenantKey.FromExternal("tenant-a");

        var http = CreateHttpContext(
            authenticated: true,
            userKey: CreateUserKey(),
            tenant: tenant);

        var result =
            ResourceAccessContextBuilder.Create(
                http,
                "users.read");

        result.ActorTenant.Should().Be(tenant);
        result.ResourceTenant.Should().Be(tenant);
    }

    [Fact]
    public void Create_WithoutTenantContext_ShouldThrow()
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

        var act = () =>
            ResourceAccessContextBuilder.Create(
                http,
                "users.read");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*TenantContext is missing*");
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

        var services = new ServiceCollection();

        services.AddSingleton(currentUser.Object);

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