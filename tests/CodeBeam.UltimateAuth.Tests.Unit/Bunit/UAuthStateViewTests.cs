using Bunit;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Reference;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthStateViewTests
{
    private IRenderedComponent<UAuthStateView> RenderWithAuth(BunitContext ctx, UAuthState state, Action<ComponentParameterCollectionBuilder<UAuthStateView>> parameters)
    {
        var wrapper = ctx.Render<CascadingValue<UAuthState>>(p => p
            .Add(x => x.Value, state)
            .AddChildContent<UAuthStateView>(parameters)
        );

        return wrapper.FindComponent<UAuthStateView>();
    }

    private static RenderFragment Html(string html)
        => b => b.AddMarkupContent(0, html);

    [Fact]
    public void Should_Render_NotAuthorized_When_Not_Authenticated()
    {
        using var ctx = new BunitContext();
        var state = UAuthState.Anonymous();
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = ctx.Render<CascadingValue<UAuthState>>(parameters => parameters
            .Add(p => p.Value, state)
            .AddChildContent<UAuthStateView>(child => child
                .Add(p => p.NotAuthorized, (RenderFragment)(b => b.AddMarkupContent(0, "<div>nope</div>")))
            )
        );

        cut.Markup.Should().Contain("nope");
    }

    [Fact]
    public void Should_Render_ChildContent_When_Authorized_Without_Conditions()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.Authenticated();
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.ChildContent, s => b => b.AddContent(0, "authorized"))
        );

        cut.Markup.Should().Contain("authorized");
    }

    [Fact]
    public void Should_Render_NotAuthorized_When_Role_Not_Match()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.WithRoles("user");
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.Roles, "admin")
            .Add(x => x.NotAuthorized, Html("<div>nope</div>"))
        );

        cut.Markup.Should().Contain("nope");
    }

    [Fact]
    public void Should_Render_Authorized_When_Role_Matches()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.WithRoles("admin");
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.Roles, "admin")
            .Add(x => x.Authorized, s => b => b.AddContent(0, "ok"))
        );

        cut.Markup.Should().Contain("ok");
    }

    [Fact]
    public void Should_Check_Permissions()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.WithPermissions("read");
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.Permissions, "write")
            .Add(x => x.NotAuthorized, Html("<div>no</div>"))
        );

        cut.Markup.Should().Contain("no");
    }

    [Fact]
    public void Should_Require_All_When_MatchAll_True()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.WithRoles("admin");
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.Roles, "admin,user")
            .Add(x => x.MatchAll, true)
            .Add(x => x.NotAuthorized, Html("<div>no</div>"))
        );

        cut.Markup.Should().Contain("no");
    }

    [Fact]
    public void Should_Allow_Any_When_MatchAll_False()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.WithRoles("admin");
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.Roles, "admin,user")
            .Add(x => x.MatchAll, false)
            .Add(x => x.Authorized, s => b => b.AddContent(0, "ok"))
        );

        cut.Markup.Should().Contain("ok");
    }

    [Fact]
    public void Should_Render_Inactive_When_Session_Not_Active()
    {
        using var ctx = new BunitContext();
        var state = TestAuthState.WithSession(SessionState.Revoked);
        ctx.Services.AddSingleton(Mock.Of<IAuthorizationService>());

        var cut = RenderWithAuth(ctx, state, p => p
            .Add(x => x.Inactive, s => b => b.AddContent(0, "inactive"))
        );

        cut.Markup.Should().Contain("inactive");
    }
}
