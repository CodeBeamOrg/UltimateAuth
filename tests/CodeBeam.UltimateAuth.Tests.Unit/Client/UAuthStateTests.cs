using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using System.Security.Claims;
using Xunit;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthStateTests
{
    private static AuthStateSnapshot CreateSnapshot()
    {
        var identity = new AuthIdentitySnapshot
        {
            UserKey = UserKey.FromGuid(Guid.NewGuid()),
            Tenant = TenantKey.FromInternal("__single__"),
            PrimaryUserName = "admin",
            AuthenticatedAt = DateTimeOffset.UtcNow
        };

        var claims = ClaimsSnapshot.From((ClaimTypes.Role, "Admin"), ("uauth:permission", "*"));

        return new AuthStateSnapshot
        {
            Identity = identity,
            Claims = claims
        };
    }

    [Fact]
    public void Anonymous_should_not_be_authenticated()
    {
        var state = UAuthState.Anonymous();

        state.IsAuthenticated.Should().BeFalse();
        state.Identity.Should().BeNull();
    }

    [Fact]
    public void ApplySnapshot_should_set_identity_and_claims()
    {
        var state = UAuthState.Anonymous();
        var snapshot = CreateSnapshot();

        state.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);

        state.IsAuthenticated.Should().BeTrue();
        state.Identity.Should().NotBeNull();
        state.Claims.Should().BeEquivalentTo(snapshot.Claims);
        state.IsStale.Should().BeFalse();
    }

    [Fact]
    public void Clear_should_reset_state()
    {
        var state = UAuthState.Anonymous();
        var snapshot = CreateSnapshot();

        state.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);
        state.Clear();

        state.IsAuthenticated.Should().BeFalse();
        state.Identity.Should().BeNull();
        state.Claims.Should().Be(ClaimsSnapshot.Empty);
    }

    [Fact]
    public void IsInRole_should_return_true_when_role_exists()
    {
        var state = UAuthState.Anonymous();
        var snapshot = CreateSnapshot();

        state.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);

        state.IsInRole("Admin").Should().BeTrue();
        state.IsInRole("User").Should().BeFalse();
    }

    [Fact]
    public void ToClaimsPrincipal_should_map_identity_correctly()
    {
        var state = UAuthState.Anonymous();
        var snapshot = CreateSnapshot();

        state.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);

        var principal = state.ToClaimsPrincipal();

        principal.Identity!.Name.Should().Be("admin");
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(snapshot.Identity.UserKey.Value);
        principal.IsInRole("Admin").Should().BeTrue();
    }
}
