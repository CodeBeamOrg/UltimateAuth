using Bunit;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages.Dashboard;
using CodeBeam.UltimateAuth.Users.Contracts;
using Moq;
using MudBlazor;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Rendering;

[Trait("Category", "Render")]
public sealed class PageRenderTests
{
    [Fact]
    [SmokeTestFor(typeof(LoginPage))]
    public async Task Anonymous_login_renders_the_real_login_form()
    {
        await using var context = new PageRenderContext();
        var page = context.RenderPage<LoginPage>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Welcome back.", page.Find("h1").TextContent);
            Assert.NotNull(page.FindComponent<UAuthLoginForm>().Find("form"));
            Assert.Contains("Email or Username", page.Markup);
        });
    }

    [Fact]
    public async Task Authenticated_login_renders_workspace_entry_instead_of_form()
    {
        await using var context = new PageRenderContext();
        var page = context.RenderPage<LoginPage>(PageRenderContext.Authenticated("Owner"));

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Enter your workspace", page.Markup);
            Assert.Empty(page.FindComponents<UAuthLoginForm>());
        });
    }

    [Fact]
    [SmokeTestFor(typeof(RegisterPage))]
    public async Task Anonymous_registration_renders_without_an_existing_session()
    {
        await using var context = new PageRenderContext();
        context.AddLabServices();
        var page = context.RenderPage<RegisterPage>();

        page.WaitForAssertion(() =>
        {
            Assert.Equal("Create your sandbox.", page.Find("h1").TextContent.Trim());
            Assert.NotNull(page.FindComponent<MudForm>());
            Assert.NotEmpty(page.FindAll("input"));
        });
    }

    [Fact]
    [SmokeTestFor(typeof(LandingPage))]
    public async Task Landing_page_renders_registration_entry()
    {
        await using var context = new PageRenderContext();
        var page = context.RenderPage<LandingPage>();
        Assert.Contains(page.FindAll("a"), link =>
            link.GetAttribute("href") == "/register" && link.TextContent.Contains("Create Your Sandbox"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SmokeTestFor(typeof(DashboardPage))]
    public async Task Dashboard_renders_owner_and_member_navigation(bool owner)
    {
        await using var context = new PageRenderContext();
        context.AddLabServices();
        context.Users.Setup(x => x.GetMeAsync(It.IsAny<GetProfileRequest>()))
            .ReturnsAsync(PageRenderContext.Success(new UserView { DisplayName = "Render Person" }));
        context.Sessions.Setup(x => x.GetMyChainsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(PageRenderContext.Success(PageRenderContext.Empty<SessionChainSummary>()));
        context.Authorization.Setup(x => x.GetMyRolesAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(PageRenderContext.Success(new UserRolesGetResult
            {
                UserKey = CodeBeam.UltimateAuth.Core.Domain.UserKey.FromString("render-user"),
                Roles = PageRenderContext.Empty<UserRoleInfo>()
            }));
        await context.SeedSandboxAsync(owner);

        var page = context.RenderPage<DashboardPage>(PageRenderContext.Authenticated(owner ? "Owner" : "User"));
        page.WaitForAssertion(() =>
        {
            Assert.Contains("Your identity workspace.", page.Find("h1").TextContent);
            Assert.Contains("Render sandbox", page.Markup);
            Assert.Contains("Render Person", page.Markup);
            Assert.Equal(owner, page.FindAll("a").Any(x => x.GetAttribute("href") == "/dashboard/users"));
            Assert.Equal(owner, page.FindAll("a").Any(x => x.GetAttribute("href") == "/dashboard/settings"));
        });
        context.Users.Verify(x => x.GetMeAsync(It.IsAny<GetProfileRequest>()), Times.Once);
        context.Sessions.Verify(x => x.GetMyChainsAsync(It.IsAny<PageRequest>()), Times.Once);
        context.Authorization.Verify(x => x.GetMyRolesAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    [SmokeTestFor(typeof(MySessionsPage))]
    public async Task Sessions_render_empty_service_result()
    {
        await using var context = new PageRenderContext();
        context.Sessions.Setup(x => x.GetMyChainsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(PageRenderContext.Success(PageRenderContext.Empty<SessionChainSummary>()));
        var page = context.RenderPage<MySessionsPage>(PageRenderContext.Authenticated("User"));

        page.WaitForAssertion(() => Assert.Contains("No session chains were returned.", page.Markup));
        Assert.Equal("My Sessions", page.Find("h1").TextContent.Trim());
        context.Sessions.Verify(x => x.GetMyChainsAsync(It.IsAny<PageRequest>()), Times.Once);
    }

    [Fact]
    public async Task Sessions_render_loading_then_explicit_service_failure()
    {
        await using var context = new PageRenderContext();
        var response = new TaskCompletionSource<UAuthResult<PagedResult<SessionChainSummary>>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        context.Sessions.Setup(x => x.GetMyChainsAsync(It.IsAny<PageRequest>())).Returns(response.Task);
        var page = context.RenderPage<MySessionsPage>(PageRenderContext.Authenticated("User"));
        Assert.NotEmpty(page.FindComponents<MudProgressLinear>());

        response.SetResult(new UAuthResult<PagedResult<SessionChainSummary>> { IsSuccess = false, Status = 503 });
        page.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load session chains.", page.Markup);
            Assert.Empty(page.FindComponents<MudProgressLinear>());
        });
    }

    [Theory]
    [InlineData(false, "No roles available.")]
    [InlineData(true, "Authorization service is unavailable.")]
    [SmokeTestFor(typeof(AuthorizationPage))]
    public async Task Authorization_management_renders_empty_and_failure_states(bool failure, string expected)
    {
        await using var context = new PageRenderContext();
        var query = context.Authorization.Setup(x => x.QueryRolesAsync(It.IsAny<RoleQuery>()));
        if (failure)
            query.ThrowsAsync(new HttpRequestException("Simulated unavailable service"));
        else
            query.ReturnsAsync(PageRenderContext.Success(PageRenderContext.Empty<RoleInfo>()));

        var page = context.RenderPage<AuthorizationPage>(PageRenderContext.Authenticated("Owner"));
        page.WaitForAssertion(() => Assert.Contains(expected, page.Markup));
        Assert.Equal("Authorization Management", page.Find("h1").TextContent.Trim());
        context.Authorization.Verify(x => x.QueryRolesAsync(It.IsAny<RoleQuery>()), Times.Once);
    }
}
