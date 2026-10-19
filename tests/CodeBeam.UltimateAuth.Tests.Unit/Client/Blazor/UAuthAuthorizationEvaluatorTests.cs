using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Authorization;
using Moq;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthAuthorizationEvaluatorTests
{
    private readonly Mock<IAuthorizationService> _authorization = new();

    private UAuthAuthorizationEvaluator CreateEvaluator() => new(_authorization.Object);

    [Fact]
    public async Task AnonymousUser_IsDenied()
    {
        var result = await CreateEvaluator().EvaluateAsync(TestAuthState.Anonymous(), new UAuthAuthorizationRequirement());
        result.Status.Should().Be(UAuthAuthorizationStatus.Denied);
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutRequirements_IsAuthorized()
    {
        var result = await CreateEvaluator().EvaluateAsync(TestAuthState.Authenticated(), new UAuthAuthorizationRequirement());
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Category_RequiresRoleAndPermission()
    {
        var requirement = new UAuthAuthorizationRequirement
        {
            Roles = "Admin,Manager",
            Permissions = "users.read,users.write"
        };

        var authorized = await CreateEvaluator().EvaluateAsync(
            TestAuthState.Create(
                roles: ["Manager"],
                permissions: ["users.read"]),
            requirement);

        var denied = await CreateEvaluator().EvaluateAsync(
            TestAuthState.Create(
                roles: ["Manager"],
                permissions: ["users.list"]),
            requirement);

        authorized.IsAuthorized.Should().BeTrue();
        denied.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Csv_IgnoresWhitespaceAndEmptyValues()
    {
        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.WithRoles("Admin"),
            new UAuthAuthorizationRequirement
            {
                Roles = " User, , Admin, "
            });

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Any_AllowsMatchingPermissionWhenRoleFails()
    {
        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.Create(
                roles: ["User"],
                permissions: ["users.read"]),
            new UAuthAuthorizationRequirement
            {
                Roles = "Admin",
                Permissions = "users.read",
                MatchMode = AuthorizationMatchMode.Any
            });

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task All_RequiresEveryRoleAndPermission()
    {
        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.Create(
                roles: ["Admin"],
                permissions: ["users.read"]),
            new UAuthAuthorizationRequirement
            {
                Roles = "Admin,Manager",
                Permissions = "users.read",
                MatchMode = AuthorizationMatchMode.All
            });

        result.Status.Should().Be(UAuthAuthorizationStatus.Denied);
    }

    [Fact]
    public async Task RevokedSession_WhenRequireActive_ReturnsInactive()
    {
        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.WithSession(SessionState.Revoked),
            new UAuthAuthorizationRequirement
            {
                RequireActive = true
            });

        result.Status.Should().Be(UAuthAuthorizationStatus.Inactive);
    }

    [Fact]
    public async Task RevokedSession_WhenActiveNotRequired_CanAuthorize()
    {
        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.WithSession(SessionState.Revoked),
            new UAuthAuthorizationRequirement
            {
                RequireActive = false
            });

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task SuccessfulPolicy_Authorizes()
    {
        _authorization
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                "CanManageUsers"))
            .ReturnsAsync(AuthorizationResult.Success());

        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.Authenticated(),
            new UAuthAuthorizationRequirement
            {
                Policy = "CanManageUsers"
            });

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task FailedPolicy_Denies()
    {
        _authorization
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                "CanManageUsers"))
            .ReturnsAsync(AuthorizationResult.Failed());

        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.Authenticated(),
            new UAuthAuthorizationRequirement
            {
                Policy = "CanManageUsers"
            });

        result.Status.Should().Be(UAuthAuthorizationStatus.Denied);
    }

    [Fact]
    public async Task InactiveSession_DoesNotEvaluatePolicy()
    {
        var result = await CreateEvaluator().EvaluateAsync(
            TestAuthState.WithSession(SessionState.Revoked),
            new UAuthAuthorizationRequirement
            {
                Policy = "CanManageUsers",
                RequireActive = true
            });

        result.IsInactive.Should().BeTrue();

        _authorization.Verify(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<object?>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Policy_ReceivesClaimsFromUAuthState()
    {
        ClaimsPrincipal? captured = null;

        _authorization
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                "CanManageUsers"))
            .Callback<ClaimsPrincipal, object?, string>(
                (principal, _, _) => captured = principal)
            .ReturnsAsync(AuthorizationResult.Success());

        await CreateEvaluator().EvaluateAsync(
            TestAuthState.Create(
                roles: ["Admin"],
                permissions: ["users.read"]),
            new UAuthAuthorizationRequirement
            {
                Policy = "CanManageUsers"
            });

        captured.Should().NotBeNull();
        captured!.IsInRole("Admin").Should().BeTrue();
        captured.HasClaim(
            "uauth:permission", "users.read").Should().BeTrue();
    }
}
