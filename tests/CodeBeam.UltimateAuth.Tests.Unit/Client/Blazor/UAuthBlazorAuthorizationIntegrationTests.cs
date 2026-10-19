using FluentAssertions;
using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Integration.Client.Blazor;

public sealed class UAuthBlazorAuthorizationIntegrationTests : BunitContext
{
    public UAuthBlazorAuthorizationIntegrationTests()
    {
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();

        Services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
        Services.AddScoped<IUAuthAuthorizationEvaluator, UAuthAuthorizationEvaluator>();
    }

    [Fact]
    public void AnonymousUser_CannotInstantiateProtectedPage()
    {
        ProtectedPage.Reset();

        var state = UAuthState.Anonymous();

        var cut = RenderRoute<ProtectedPage>(state);

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".not-authorized").Should().ContainSingle();
            cut.FindAll(".protected-page").Should().BeEmpty();

            ProtectedPage.CreatedCount.Should().Be(0);
        });
    }

    [Fact]
    public void Login_GrantsAccessToProtectedPage()
    {
        ProtectedPage.Reset();

        var state = UAuthState.Anonymous();

        var cut = RenderRoute<ProtectedPage>(state);

        cut.WaitForAssertion(() =>
            cut.FindAll(".not-authorized").Should().ContainSingle());

        ApplyAuthenticatedSnapshot(
            state,
            roles: ["Admin"],
            permissions: ["users.write"]);

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".protected-page").Should().ContainSingle();
            cut.FindAll(".not-authorized").Should().BeEmpty();

            ProtectedPage.CreatedCount.Should().Be(1);
        });
    }

    [Fact]
    public void Logout_RemovesPreviouslyAuthorizedPage()
    {
        ProtectedPage.Reset();

        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.write"]);

        var cut = RenderRoute<ProtectedPage>(state);

        cut.WaitForAssertion(() =>
            cut.FindAll(".protected-page").Should().ContainSingle());

        state.Clear();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".protected-page").Should().BeEmpty();
            cut.FindAll(".not-authorized").Should().ContainSingle();
        });

        // The page must no longer exist in the component tree.
        cut.FindComponents<ProtectedPage>().Should().BeEmpty();
    }

    [Fact]
    public void RouteAndSection_ProduceConsistentAuthorizationDecisions()
    {
        var authorizedState = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.write"]);

        var deniedState = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.read"]);

        AssertConsistentDecision(authorizedState, expected: true);
        AssertConsistentDecision(deniedState, expected: false);
    }

    private void AssertConsistentDecision(UAuthState state, bool expected)
    {
        using var route = RenderRoute<ProtectedPage>(state);
        using var section = Render<SectionHost>(parameters =>
            parameters.Add(x => x.State, state));

        route.WaitForAssertion(() =>
        {
            route.FindAll(".protected-page").Any()
                .Should().Be(expected);
        });

        section.WaitForAssertion(() =>
        {
            section.FindAll(".authorized-section").Any()
                .Should().Be(expected);
        });
    }

    private IRenderedComponent<RouteHost> RenderRoute<TPage>(
        UAuthState state)
        where TPage : IComponent
    {
        return Render<RouteHost>(parameters => parameters
            .Add(x => x.PageType, typeof(TPage))
            .Add(x => x.State, state));
    }

    private sealed class RouteHost : ComponentBase
    {
        [Parameter]
        public Type PageType { get; set; } = default!;

        [Parameter]
        public UAuthState State { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<UAuthState>>(0);
            builder.AddAttribute(1, "Value", State);
            builder.AddAttribute(2, "ChildContent",
                (RenderFragment)(content =>
                {
                    content.OpenComponent<CascadingValue<bool>>(0);
                    content.AddAttribute(1, "Name", "UAuthRuntimeReady");
                    content.AddAttribute(2, "Value", true);
                    content.AddAttribute(3, "ChildContent",
                        (RenderFragment)(inner =>
                        {
                            inner.OpenComponent<UAuthAuthorizeRouteView>(0);
                            inner.AddAttribute(
                                1,
                                "RouteData",
                                new RouteData(PageType,
                                    new Dictionary<string, object?>()));

                            inner.AddAttribute(
                                2,
                                "NotAuthorized",
                                (RenderFragment<AuthenticationState>)(_ =>
                                    denied =>
                                    {
                                        denied.OpenElement(0, "div");
                                        denied.AddAttribute(
                                            1, "class", "not-authorized");
                                        denied.AddContent(2, "Access denied");
                                        denied.CloseElement();
                                    }));

                            inner.CloseComponent();
                        }));
                    content.CloseComponent();
                }));
            builder.CloseComponent();
        }
    }

    private sealed class SectionHost : ComponentBase
    {
        [Parameter]
        public UAuthState State { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<UAuthState>>(0);
            builder.AddAttribute(1, "Value", State);
            builder.AddAttribute(2, "ChildContent",
                (RenderFragment)(content =>
                {
                    content.OpenComponent<UAuthStateView>(0);
                    content.AddAttribute(1, "Roles", "Admin");
                    content.AddAttribute(2, "Permissions", "users.write");
                    content.AddAttribute(
                        3,
                        "RequireActive",
                        false);

                    content.AddAttribute(
                        4,
                        "Authorized",
                        (RenderFragment<UAuthState>)(_ => authorized =>
                        {
                            authorized.OpenElement(0, "div");
                            authorized.AddAttribute(1, "class", "authorized-section");
                            authorized.AddContent(2, "Authorized");
                            authorized.CloseElement();
                        }));

                    content.CloseComponent();
                }));
            builder.CloseComponent();
        }
    }

    [UAuthAuthorize(
        Roles = "Admin",
        Permissions = "users.write",
        RequireActive = false)]
    private sealed class ProtectedPage : ComponentBase
    {
        public static int CreatedCount { get; private set; }

        public ProtectedPage()
        {
            CreatedCount++;
        }

        public static void Reset()
        {
            CreatedCount = 0;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "protected-page");
            builder.AddContent(2, "Protected content");
            builder.CloseElement();
        }
    }

    private static UAuthState AuthenticatedState(
        string[]? roles = null,
        string[]? permissions = null)
    {
        var state = UAuthState.Anonymous();

        ApplyAuthenticatedSnapshot(state, roles, permissions);

        return state;
    }

    private static void ApplyAuthenticatedSnapshot(
        UAuthState state,
        string[]? roles = null,
        string[]? permissions = null)
    {
        var claims = new List<(string Type, string Value)>();

        foreach (var role in roles ?? [])
            claims.Add((System.Security.Claims.ClaimTypes.Role, role));

        foreach (var permission in permissions ?? [])
            claims.Add(("uauth:permission", permission));

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
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            return Task.FromResult(new AuthenticationState(principal));
        }
    }
}
