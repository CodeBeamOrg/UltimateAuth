using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthComponentBaseTests : BunitContext
{
    [Fact]
    public void Render_WithoutUAuthState_ThrowsInvalidOperationException()
    {
        var act = () => Render<TestComponent>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*requires a cascading parameter*UAuthState*");
    }

    [Fact]
    public void Render_WithUAuthState_ReceivesCascadingState()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderWithState<TestComponent>(state);

        var component = cut
            .FindComponent<TestComponent>()
            .Instance;

        component.CurrentAuthState
            .Should()
            .BeSameAs(state);
    }

    [Fact]
    public void Render_WithoutAuthorizeAttribute_DoesNotCallUnauthorized()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderWithState<TestComponent>(state);

        var component = cut
            .FindComponent<TestComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithAuthorizeAttribute_WhenAnonymous_CallsUnauthorized()
    {
        var state = UAuthState.Anonymous();

        var cut =
            RenderWithState<AuthenticationRequiredComponent>(state);

        var component = cut
            .FindComponent<AuthenticationRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(1);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithRoleRequirement_WhenAnonymous_CallsUnauthorized_NotForbidden()
    {
        var state = UAuthState.Anonymous();

        var cut =
            RenderWithState<AdminRequiredComponent>(state);

        var component = cut
            .FindComponent<AdminRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(1);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithPermissionRequirement_WhenAnonymous_CallsUnauthorized_NotForbidden()
    {
        var state = UAuthState.Anonymous();

        var cut =
            RenderWithState<ReadUsersRequiredComponent>(state);

        var component = cut
            .FindComponent<ReadUsersRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(1);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderWithState<TestComponent>(state);

        var act = () => cut.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Render_WithAuthorizeAttribute_WhenAuthenticated_DoesNotReject()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<AuthenticationRequiredComponent>(state);

        var component = cut
            .FindComponent<AuthenticationRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithRequiredRole_WhenUserHasRole_DoesNotReject()
    {
        var state = AuthenticatedState(
            roles: ["admin"]);

        var cut = RenderWithState<AdminRequiredComponent>(state);

        var component = cut
            .FindComponent<AdminRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithRequiredRole_WhenUserDoesNotHaveRole_CallsForbidden()
    {
        var state = AuthenticatedState(
            roles: ["member"]);

        var cut = RenderWithState<AdminRequiredComponent>(state);

        var component = cut
            .FindComponent<AdminRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(1);
    }

    [Fact]
    public void Render_WithRequiredPermission_WhenUserHasPermission_DoesNotReject()
    {
        var state = AuthenticatedState(
            permissions: ["users.read"]);

        var cut = RenderWithState<ReadUsersRequiredComponent>(state);

        var component = cut
            .FindComponent<ReadUsersRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithRequiredPermission_WhenUserDoesNotHavePermission_CallsForbidden()
    {
        var state = AuthenticatedState(
            permissions: ["users.list"]);

        var cut = RenderWithState<ReadUsersRequiredComponent>(state);

        var component = cut
            .FindComponent<ReadUsersRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(1);
    }

    [Fact]
    public void Render_WithMultipleRoles_AllowsWhenAnyRoleMatches()
    {
        var state = AuthenticatedState(
            roles: ["manager"]);

        var cut = RenderWithState<AdminOrManagerComponent>(state);

        var component = cut
            .FindComponent<AdminOrManagerComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithMultiplePermissions_AllowsWhenAnyPermissionMatches()
    {
        var state = AuthenticatedState(
            permissions: ["users.write"]);

        var cut = RenderWithState<ReadOrWriteUsersComponent>(state);

        var component = cut
            .FindComponent<ReadOrWriteUsersComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);
        component.ForbiddenCount.Should().Be(0);
    }

    [Fact]
    public void StateChange_ForwardsReasonToComponent()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);

        var component = cut
            .FindComponent<TestComponent>()
            .Instance;

        state.MarkStale();

        component.StateChangedCount.Should().Be(1);
        component.LastChangeReason
            .Should().Be(UAuthStateChangeReason.MarkedStale);
    }

    [Fact]
    public void StateChange_ReevaluatesAuthorization()
    {
        var state = AuthenticatedState();

        var cut =
            RenderWithState<AuthenticationRequiredComponent>(state);

        var component = cut
            .FindComponent<AuthenticationRequiredComponent>()
            .Instance;

        component.UnauthorizedCount.Should().Be(0);

        state.Clear();

        component.UnauthorizedCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_UnsubscribesFromAuthState()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);

        var component = cut
            .FindComponent<TestComponent>()
            .Instance;

        state.MarkStale();

        component.StateChangedCount.Should().Be(1);

        component.Dispose();

        state.MarkValidated(DateTimeOffset.UtcNow);

        component.StateChangedCount.Should().Be(1);
        component.LastChangeReason
            .Should().Be(UAuthStateChangeReason.MarkedStale);
    }

    private IRenderedComponent<CascadingValue<UAuthState>> RenderWithState<TComponent>(UAuthState state) where TComponent : IComponent
    {
        return Render<CascadingValue<UAuthState>>(parameters =>
            parameters
                .Add(x => x.Value, state)
                .AddChildContent<TComponent>());
    }

    [UAuthAuthorize]
    private sealed class AuthenticationRequiredComponent
        : TestComponent
    {
    }

    [UAuthAuthorize(Roles = "admin")]
    private sealed class AdminRequiredComponent
        : TestComponent
    {
    }

    [UAuthAuthorize(Permissions = "users.read")]
    private sealed class ReadUsersRequiredComponent
        : TestComponent
    {
    }

    [UAuthAuthorize(Roles = "admin,manager")]
    private sealed class AdminOrManagerComponent : TestComponent
    {
    }

    [UAuthAuthorize(Permissions = "users.read,users.write")]
    private sealed class ReadOrWriteUsersComponent : TestComponent
    {
    }


    private class TestComponent : UAuthComponentBase
    {
        public int StateChangedCount { get; private set; }

        public UAuthStateChangeReason? LastChangeReason { get; private set; }

        public int UnauthorizedCount { get; private set; }

        public int ForbiddenCount { get; private set; }

        public UAuthState CurrentAuthState => AuthState;

        protected override void HandleAuthStateChanged(
            UAuthStateChangeReason reason)
        {
            StateChangedCount++;
            LastChangeReason = reason;
        }

        protected override void OnUnauthorized()
        {
            UnauthorizedCount++;
        }

        protected override void OnForbidden()
        {
            ForbiddenCount++;
        }

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            builder.AddContent(0, "uauth-test-component");
        }
    }

    private static UAuthState AuthenticatedState(string[]? roles = null, string[]? permissions = null)
    {
        var claims = new List<(string Type, string Value)>();

        foreach (var role in roles ?? [])
            claims.Add((ClaimTypes.Role, role));

        foreach (var permission in permissions ?? [])
            claims.Add(("uauth:permission", permission));

        var state = UAuthState.Anonymous();

        state.ApplySnapshot(
            new AuthStateSnapshot
            {
                Identity = new AuthIdentitySnapshot
                {
                    UserKey = UserKey.New(),
                    Tenant = TenantKey.Single,
                    PrimaryUserName = "alice"
                },
                Claims = ClaimsSnapshot.From(claims.ToArray())
            },
            DateTimeOffset.UtcNow);

        return state;
    }
}
