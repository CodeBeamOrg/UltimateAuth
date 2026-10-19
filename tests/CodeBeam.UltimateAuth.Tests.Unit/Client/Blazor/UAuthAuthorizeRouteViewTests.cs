using FluentAssertions;
using Bunit;
using Bunit.TestDoubles;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

/// <summary>
/// Contract tests for UltimateAuth route authorization. These exercise the
/// actual route view, rather than duplicating its authorization algorithm.
/// </summary>
public sealed class UAuthAuthorizeRouteViewTests : BunitContext
{
    private const string Denied = "ROUTE-DENIED";
    private const string Loading = "ROUTE-LOADING";

    private readonly BunitAuthorizationContext _authorization;

    public UAuthAuthorizeRouteViewTests()
    {
        Services.AddScoped<IUAuthAuthorizationEvaluator, UAuthAuthorizationEvaluator>();

        _authorization = AddAuthorization();
        _authorization.SetNotAuthorized();
    }

    [Fact]
    public void Anonymous_ProtectedRoute_DoesNotInstantiatePage()
    {
        var cut = RenderRoute<ProtectedPage>(TestAuthState.Anonymous());
        AssertDenied<ProtectedPage>(cut);
    }

    [Fact]
    public void Authenticated_ProtectedRoute_RendersPage_WithoutBaseClass()
    {
        var cut = RenderRoute<ProtectedPage>(TestAuthState.Authenticated());
        AssertVisible<ProtectedPage>(cut);
    }

    [Fact]
    public void ProtectedRoute_BeforeRuntimeReady_DoesNotInstantiatePage()
    {
        var cut = RenderRoute<ProtectedPage>(TestAuthState.Authenticated(), ready: false);

        cut.Markup.Should().Contain(Loading);
        cut.FindComponents<ProtectedPage>().Should().BeEmpty();
    }

    [Fact]
    public void Anonymous_ProtectedRoute_BeforeRuntimeReady_ShowsLoadingNotDenied()
    {
        var cut = RenderRoute<ProtectedPage>(TestAuthState.Anonymous(), ready: false);

        cut.Markup.Should().Contain(Loading);
        cut.Markup.Should().NotContain(Denied);
        cut.FindComponents<ProtectedPage>().Should().BeEmpty();
    }

    [Fact]
    public void RuntimeBecomesReady_ReevaluatesProtectedRoute()
    {
        var state = TestAuthState.Authenticated();
        var cut = RenderHost(typeof(ProtectedPage), state, ready: false);

        cut.FindComponents<ProtectedPage>().Should().BeEmpty();
        cut.Render(p => p
            .Add(x => x.PageType, typeof(ProtectedPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, true));

        AssertVisible<ProtectedPage>(cut);
    }

    [Fact]
    public void MissingRole_DeniesProtectedRoute()
    {
        var cut = RenderRoute<AdminPage>(TestAuthState.WithRoles("User"));
        AssertDenied<AdminPage>(cut);
    }

    [Fact]
    public void MatchingRole_AllowsProtectedRoute()
    {
        var cut = RenderRoute<AdminPage>(TestAuthState.WithRoles("Admin"));
        AssertVisible<AdminPage>(cut);
    }

    [Fact]
    public void RoleCsv_AllowsAnyMatchingRole_AndTrimsWhitespace()
    {
        var cut = RenderRoute<MultiRolePage>(TestAuthState.WithRoles("Manager"));
        AssertVisible<MultiRolePage>(cut);
    }

    [Fact]
    public void MissingPermission_DeniesProtectedRoute()
    {
        var cut = RenderRoute<PermissionPage>(TestAuthState.WithPermissions("users.list"));
        AssertDenied<PermissionPage>(cut);
    }

    [Fact]
    public void MatchingPermission_AllowsProtectedRoute()
    {
        var cut = RenderRoute<PermissionPage>(TestAuthState.WithPermissions("users.read"));
        AssertVisible<PermissionPage>(cut);
    }

    [Fact]
    public void RoleAndPermission_RequireMatchInBothCategories()
    {
        var onlyRole = RenderRoute<RoleAndPermissionPage>(TestAuthState.Create(roles: ["Admin"], permissions: ["users.list"]));

        AssertDenied<RoleAndPermissionPage>(onlyRole);

        var both = RenderRoute<RoleAndPermissionPage>(TestAuthState.Create(roles: ["Admin"], permissions: ["users.read"]));

        AssertVisible<RoleAndPermissionPage>(both);
    }

    [Fact]
    public void PublicRoute_DoesNotRequireUltimateAuthAuthenticationOrReadiness()
    {
        var cut = RenderRoute<PublicPage>(TestAuthState.Anonymous(), ready: false);
        AssertVisible<PublicPage>(cut);
    }

    [Fact]
    public void StandardAuthorize_AnonymousUser_DeniesRoute()
    {
        var cut = RenderRoute<StandardAuthorizePage>(TestAuthState.Authenticated());
        AssertDenied<StandardAuthorizePage>(cut);
    }

    [Fact]
    public void StandardAuthorize_AuthorizedUser_AllowsRoute()
    {
        _authorization.SetAuthorized("aspnet-user");
        var cut = RenderRoute<StandardAuthorizePage>(TestAuthState.Anonymous());
        AssertVisible<StandardAuthorizePage>(cut);
    }

    [Fact]
    public void CombinedAttributes_RequireBothUltimateAuthAndAspNetAuthorization()
    {
        // ASP.NET authorized, UltimateAuth anonymous.
        _authorization.SetAuthorized("aspnet-user");

        var cut = RenderRoute<CombinedPage>(
            TestAuthState.Anonymous());

        AssertDenied<CombinedPage>(cut);

        // UltimateAuth authorized, ASP.NET unauthorized.
        _authorization.SetNotAuthorized();

        cut.Render(p => p
            .Add(x => x.PageType, typeof(CombinedPage))
            .Add(x => x.State, TestAuthState.WithRoles("Admin"))
            .Add(x => x.Ready, true));

        AssertDenied<CombinedPage>(cut);
    }

    [Fact]
    public void RouteChange_ReevaluatesAttributeAndPreventsOldPageRendering()
    {
        var state = TestAuthState.WithRoles("User");
        var cut = RenderHost(typeof(PublicPage), state);
        AssertVisible<PublicPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(AdminPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, true));

        AssertDenied<AdminPage>(cut);
        cut.FindComponents<PublicPage>().Should().BeEmpty();
    }

    [Fact]
    public void RouteChange_FromDeniedToPublic_RendersPublicPage()
    {
        var state = TestAuthState.Anonymous();
        var cut = RenderHost(typeof(ProtectedPage), state);
        AssertDenied<ProtectedPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(PublicPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, true));

        AssertVisible<PublicPage>(cut);
    }

    [Fact]
    public void Logout_RemovesPreviouslyAuthorizedPage()
    {
        var state = TestAuthState.Authenticated();
        var cut = RenderRoute<ProtectedPage>(state);
        AssertVisible<ProtectedPage>(cut);

        cut.InvokeAsync(state.Clear);
        cut.WaitForAssertion(() => AssertDenied<ProtectedPage>(cut));
    }

    [Fact]
    public void Login_ShowsPreviouslyDeniedPage()
    {
        var state = TestAuthState.Anonymous();
        var cut = RenderRoute<ProtectedPage>(state);
        AssertDenied<ProtectedPage>(cut);

        // TestAuthState.Authenticated creates a snapshot; applying it to the
        // SAME state instance tests the Changed event subscription.
        var authenticated = TestAuthState.Authenticated();
        cut.InvokeAsync(() => state.ApplySnapshot(
            new AuthStateSnapshot
            {
                Identity = authenticated.Identity ?? throw new InvalidOperationException("Authenticated test state must contain an identity."),
                Claims = authenticated.Claims
            }, DateTimeOffset.UtcNow));

        cut.WaitForAssertion(() => AssertVisible<ProtectedPage>(cut));
    }

    [Fact]
    public void CombinedAttributes_WhenBothAuthorized_RendersPage()
    {
        _authorization.SetAuthorized("aspnet-user");

        var cut = RenderRoute<CombinedPage>(TestAuthState.WithRoles("Admin"));

        AssertVisible<CombinedPage>(cut);
    }

    [Fact]
    public void ReadinessDrops_AuthorizedRouteIsImmediatelyRemoved()
    {
        var state = TestAuthState.Authenticated();
        var cut = RenderRoute<ProtectedPage>(state);
        AssertVisible<ProtectedPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(ProtectedPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, false));

        cut.FindComponents<ProtectedPage>().Should().BeEmpty();
        cut.Markup.Should().Contain(Loading);
    }

    [Fact]
    public void ProtectedToProtected_DoesNotKeepPreviouslyAuthorizedPage()
    {
        var state = TestAuthState.WithRoles("User");
        var cut = RenderRoute<UserPage>(state);
        AssertVisible<UserPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(AdminPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, true));

        AssertDenied<AdminPage>(cut);
        cut.FindComponents<UserPage>().Should().BeEmpty();
    }

    [Fact]
    public void ProtectedToProtected_ReevaluatesDifferentRequirements()
    {
        var state = TestAuthState.WithRoles("Admin");
        var cut = RenderRoute<UserPage>(state);
        AssertDenied<UserPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(AdminPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, true));

        AssertVisible<AdminPage>(cut);
    }

    [Fact]
    public void StateInstanceReplacement_ReevaluatesAuthorization()
    {
        var cut = RenderRoute<AdminPage>(TestAuthState.WithRoles("Admin"));
        AssertVisible<AdminPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(AdminPage))
            .Add(x => x.State, TestAuthState.WithRoles("User"))
            .Add(x => x.Ready, true));

        AssertDenied<AdminPage>(cut);
    }

    [Fact]
    public void PublicRoute_RemainsVisibleWhenReadinessChanges()
    {
        var state = TestAuthState.Anonymous();
        var cut = RenderRoute<PublicPage>(state, ready: true);
        AssertVisible<PublicPage>(cut);

        cut.Render(p => p
            .Add(x => x.PageType, typeof(PublicPage))
            .Add(x => x.State, state)
            .Add(x => x.Ready, false));

        AssertVisible<PublicPage>(cut);
    }

    [Fact]
    public void RoleRevocation_OnSameStateInstance_RemovesPage()
    {
        var state = TestAuthState.WithRoles("Admin");
        var cut = RenderRoute<AdminPage>(state);
        AssertVisible<AdminPage>(cut);

        var replacement = TestAuthState.WithRoles("User");

        cut.InvokeAsync(() => state.ApplySnapshot(new AuthStateSnapshot
        {
            Identity = replacement.Identity
                ?? throw new InvalidOperationException("Expected identity."),
            Claims = replacement.Claims
        }, DateTimeOffset.UtcNow));

        cut.WaitForAssertion(() => AssertDenied<AdminPage>(cut));
    }

    [Fact]
    public void PermissionRevocation_OnSameStateInstance_RemovesPage()
    {
        var state = TestAuthState.WithPermissions("users.read");
        var cut = RenderRoute<PermissionPage>(state);
        AssertVisible<PermissionPage>(cut);

        var replacement = TestAuthState.WithPermissions("users.list");

        cut.InvokeAsync(() => state.ApplySnapshot(new AuthStateSnapshot
        {
            Identity = replacement.Identity
                ?? throw new InvalidOperationException("Expected identity."),
            Claims = replacement.Claims
        }, DateTimeOffset.UtcNow));

        cut.WaitForAssertion(() => AssertDenied<PermissionPage>(cut));
    }

    [Fact]
    public void MultipleLoginLogoutTransitions_KeepRouteSynchronized()
    {
        var state = TestAuthState.Anonymous();
        var cut = RenderRoute<ProtectedPage>(state);
        AssertDenied<ProtectedPage>(cut);

        for (var i = 0; i < 3; i++)
        {
            var authenticated = TestAuthState.Authenticated();

            cut.InvokeAsync(() => state.ApplySnapshot(new AuthStateSnapshot
            {
                Identity = authenticated.Identity
                    ?? throw new InvalidOperationException("Expected identity."),
                Claims = authenticated.Claims
            }, DateTimeOffset.UtcNow));

            cut.WaitForAssertion(() => AssertVisible<ProtectedPage>(cut));

            cut.InvokeAsync(state.Clear);

            cut.WaitForAssertion(() => AssertDenied<ProtectedPage>(cut));
        }
    }

    [Fact]
    public void StandardRoleRequirement_WithoutMatchingRole_DeniesRoute()
    {
        _authorization.SetAuthorized("aspnet-user");

        var cut = RenderRoute<StandardRolePage>(
            TestAuthState.Authenticated());

        AssertDenied<StandardRolePage>(cut);
    }

    [Fact]
    public void StandardRoleRequirement_WithMatchingRole_AllowsRoute()
    {
        _authorization.SetAuthorized("aspnet-user");
        _authorization.SetRoles("Admin");

        var cut = RenderRoute<StandardRolePage>(
            TestAuthState.Anonymous());

        AssertVisible<StandardRolePage>(cut);
    }

    [Fact]
    public void StandardPolicyRequirement_WithoutPolicy_DeniesRoute()
    {
        _authorization.SetAuthorized("aspnet-user");

        var cut = RenderRoute<StandardPolicyPage>(
            TestAuthState.Anonymous());

        AssertDenied<StandardPolicyPage>(cut);
    }

    [Fact]
    public void StandardPolicyRequirement_WithPolicy_AllowsRoute()
    {
        _authorization.SetAuthorized("aspnet-user");
        _authorization.SetPolicies("CanManageUsers");

        var cut = RenderRoute<StandardPolicyPage>(
            TestAuthState.Anonymous());

        AssertVisible<StandardPolicyPage>(cut);
    }

    [Fact]
    public void CombinedAttributes_StandardRoleStillRequiredWhenUltimateAuthSucceeds()
    {
        _authorization.SetAuthorized("aspnet-user");

        var cut = RenderRoute<CombinedRolePage>(
            TestAuthState.WithRoles("Admin"));

        AssertDenied<CombinedRolePage>(cut);
    }

    [Fact]
    public void CombinedAttributes_BothIndependentRoleRequirementsSatisfied_AllowsRoute()
    {
        _authorization.SetAuthorized("aspnet-user");
        _authorization.SetRoles("Manager");

        var cut = RenderRoute<CombinedRolePage>(
            TestAuthState.WithRoles("Admin"));

        AssertVisible<CombinedRolePage>(cut);
    }

    [Fact]
    public void UnauthorizedPage_OnInitializedNeverRuns()
    {
        InitializationProbePage.Initializations = 0;

        var cut = RenderRoute<InitializationProbePage>(
            TestAuthState.Anonymous());

        AssertDenied<InitializationProbePage>(cut);

        InitializationProbePage.Initializations.Should().Be(0);
    }

    [Fact]
    public void AuthorizedPage_OnInitializedRunsExactlyOnce()
    {
        InitializationProbePage.Initializations = 0;

        var cut = RenderRoute<InitializationProbePage>(
            TestAuthState.Authenticated());

        AssertVisible<InitializationProbePage>(cut);

        InitializationProbePage.Initializations.Should().Be(1);
    }


    private IRenderedComponent<RouteHost> RenderRoute<TPage>(UAuthState state, bool ready = true)
        where TPage : IComponent
        => RenderHost(typeof(TPage), state, ready);

    private IRenderedComponent<RouteHost> RenderHost(Type pageType, UAuthState state, bool ready = true)
        => Render<RouteHost>(p => p
            .Add(x => x.PageType, pageType)
            .Add(x => x.State, state)
            .Add(x => x.Ready, ready));

    private static void AssertDenied<TPage>(IRenderedComponent<RouteHost> cut)
        where TPage : IComponent
    {
        cut.Markup.Should().Contain(Denied);
        cut.FindComponents<TPage>().Should().BeEmpty();
    }

    private static void AssertVisible<TPage>(IRenderedComponent<RouteHost> cut)
        where TPage : IComponent
    {
        cut.FindComponents<TPage>().Should().ContainSingle();
        cut.Markup.Should().NotContain(Denied);
        cut.Markup.Should().NotContain(Loading);
    }

    /// <summary>
    /// A minimal equivalent of UAuthApp's cascading state and readiness.
    /// Uses the REAL UAuthAuthorizeRouteView and nested AuthorizeRouteView.
    /// </summary>
    public sealed class RouteHost : ComponentBase
    {
        [Parameter] public Type PageType { get; set; } = default!;
        [Parameter] public UAuthState State { get; set; } = default!;
        [Parameter] public bool Ready { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<CascadingValue<UAuthState>>(0);
            b.AddAttribute(1, "Value", State);
            b.AddAttribute(2, "ChildContent", (RenderFragment)(outer =>
            {
                outer.OpenComponent<CascadingValue<bool>>(0);
                outer.AddAttribute(1, "Name", "UAuthRuntimeReady");
                outer.AddAttribute(2, "Value", Ready);
                outer.AddAttribute(3, "ChildContent", (RenderFragment)(inner =>
                {
                    inner.OpenComponent<UAuthAuthorizeRouteView>(0);
                    inner.AddAttribute(1, "RouteData", new RouteData(
                        PageType, new Dictionary<string, object?>()));
                    inner.AddAttribute(2, "NotAuthorized",
                        (RenderFragment<AuthenticationState>)(_ => content =>
                            content.AddContent(0, Denied)));
                    inner.AddAttribute(3, "Authorizing",
                        (RenderFragment)(content => content.AddContent(0, Loading)));
                    inner.CloseComponent();
                }));
                outer.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    public abstract class TestPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
            => b.AddContent(0, GetType().Name);
    }

    public sealed class PublicPage : TestPage { }

    [UAuthAuthorize]
    public sealed class ProtectedPage : TestPage { }

    [UAuthAuthorize(Roles = "Admin")]
    public sealed class AdminPage : TestPage { }

    [UAuthAuthorize(Roles = " User, , Manager, ")]
    public sealed class MultiRolePage : TestPage { }

    [UAuthAuthorize(Permissions = "users.read")]
    public sealed class PermissionPage : TestPage { }

    [UAuthAuthorize(Roles = "Admin", Permissions = "users.read")]
    public sealed class RoleAndPermissionPage : TestPage { }

    [Authorize]
    public sealed class StandardAuthorizePage : TestPage { }

    [Authorize]
    [UAuthAuthorize(Roles = "Admin")]
    public sealed class CombinedPage : TestPage { }

    [UAuthAuthorize(Roles = "User")]
    public sealed class UserPage : TestPage { }

    [Authorize(Roles = "Admin")]
    public sealed class StandardRolePage : TestPage { }

    [Authorize(Policy = "CanManageUsers")]
    public sealed class StandardPolicyPage : TestPage { }

    [Authorize(Roles = "Manager")]
    [UAuthAuthorize(Roles = "Admin")]
    public sealed class CombinedRolePage : TestPage { }

    [UAuthAuthorize]
    public sealed class InitializationProbePage : TestPage
    {
        public static int Initializations { get; set; }

        protected override void OnInitialized()
            => Initializations++;
    }
}
