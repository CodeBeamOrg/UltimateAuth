using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class PureJwtSnapshotFactoryTests
{
    [Fact]
    public void Create_WithProfileClaims_PopulatesIdentity()
    {
        var userKey = UserKey.New();
        var tenant = TenantKey.Single;

        var authenticatedAt = new DateTimeOffset(
            2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

        var principal = CreatePrincipal(
            new Claim(
                UAuthJwtProfileClaims.PrimaryUserName,
                "testuser"),
            new Claim(
                UAuthJwtProfileClaims.DisplayName,
                "Test User"),
            new Claim(
                UAuthJwtProfileClaims.TimeZone,
                "Europe/Istanbul"),
            new Claim(
                UAuthJwtProfileClaims.AuthenticatedAt,
                authenticatedAt.ToString("O")),
            new Claim("uauth:permission", "users.read"));

        var snapshot = PureJwtSnapshotFactory.Create(
            principal,
            userKey,
            tenant);

        snapshot.Identity.UserKey.Should().Be(userKey);
        snapshot.Identity.Tenant.Should().Be(tenant);
        snapshot.Identity.PrimaryUserName.Should().Be("testuser");
        snapshot.Identity.DisplayName.Should().Be("Test User");
        snapshot.Identity.TimeZone.Should().Be("Europe/Istanbul");
        snapshot.Identity.AuthenticatedAt.Should().Be(authenticatedAt);

        snapshot.Identity.SessionState.Should().BeNull();
        snapshot.Identity.PrimaryEmail.Should().BeNull();
        snapshot.Identity.PrimaryPhone.Should().BeNull();

        snapshot.Claims.Claims["uauth:permission"]
            .Should().Contain("users.read");
    }

    [Fact]
    public void Create_WithoutProfileClaims_ProducesValidSnapshot()
    {
        var snapshot = PureJwtSnapshotFactory.Create(
            CreatePrincipal(),
            UserKey.New(),
            TenantKey.Single);

        snapshot.Identity.PrimaryUserName.Should().BeNull();
        snapshot.Identity.DisplayName.Should().BeNull();
        snapshot.Identity.TimeZone.Should().BeNull();
        snapshot.Identity.AuthenticatedAt.Should().BeNull();
        snapshot.Identity.SessionState.Should().BeNull();
    }

    [Fact]
    public void Create_WithInvalidAuthenticatedAt_DoesNotThrow()
    {
        var principal = CreatePrincipal(
            new Claim(
                UAuthJwtProfileClaims.AuthenticatedAt,
                "invalid-date"));

        var snapshot = PureJwtSnapshotFactory.Create(
            principal,
            UserKey.New(),
            TenantKey.Single);

        snapshot.Identity.AuthenticatedAt.Should().BeNull();
    }

    private static ClaimsPrincipal CreatePrincipal(
        params Claim[] claims)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, "TestAuth"));
    }
}
