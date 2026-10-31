using Bunit;
using CodeBeam.UltimateAuth.Core.Defaults;
using AuthorizationLabPage = CodeBeam.UltimateAuth.IdentityLab.Components.Pages.Dashboard.AuthorizationLab;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages.AuthorizationLab;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages.Dashboard;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Rendering;

[Trait("Category", "Render")]
public sealed class RemainingPageRenderTests
{
    [Fact, SmokeTestFor(typeof(UsersPage))]
    public async Task Owner_can_render_sandbox_users()
    {
        await using var context = new PageRenderContext();
        context.AddManagementServices();
        await context.SeedSandboxAsync(owner: true);
        var page = AssertPage<UsersPage>(context, "Users", "Owner");
        page.WaitForAssertion(() => Assert.Contains("Render Person", page.Markup));
    }

    [Fact, SmokeTestFor(typeof(MyIdentityPage))]
    public async Task User_can_render_profile_identifiers_and_credentials()
    {
        await using var context = new PageRenderContext();
        context.AddManagementServices();
        context.SetupProfileAndAccess();
        await context.SeedSandboxAsync(owner: false);
        var page = AssertPage<MyIdentityPage>(context, "My Identity", "User");
        page.WaitForAssertion(() =>
        {
            Assert.Contains("Render Person", page.Markup);
            Assert.Contains(page.FindAll("h2"), heading => heading.TextContent == "Credentials & Security");
        });
    }

    [Fact, SmokeTestFor(typeof(MyAccessPage))]
    public async Task User_can_render_access_with_no_assigned_roles_or_permissions()
    {
        await using var context = new PageRenderContext();
        context.AddManagementServices();
        context.SetupProfileAndAccess();
        await context.SeedSandboxAsync(owner: false);
        var page = AssertPage<MyAccessPage>(context, "My Access", "User");
        page.WaitForAssertion(() =>
        {
            Assert.Contains("No roles assigned", page.Markup);
            Assert.Contains("No permissions assigned", page.Markup);
            Assert.DoesNotContain("Unable to load sandbox members.", page.Markup);
        });
    }

    [Fact, SmokeTestFor(typeof(SettingsPage))]
    public async Task Owner_can_render_sandbox_settings()
    {
        await using var context = new PageRenderContext();
        context.AddManagementServices();
        await context.SeedSandboxAsync(owner: true);
        var page = AssertPage<SettingsPage>(context, "Sandbox Settings", "Owner");
        page.WaitForAssertion(() => Assert.Contains("Render Person", page.Markup));
    }

    [Fact, SmokeTestFor(typeof(ActivityPage))]
    public async Task User_can_render_empty_sandbox_activity()
    {
        await using var context = new PageRenderContext();
        context.AddLabServices();
        await context.SeedSandboxAsync(owner: false);
        var page = AssertPage<ActivityPage>(context, "Activity", "User");
        page.WaitForAssertion(() => Assert.Contains("No activity recorded", page.Markup));
    }

    [Fact, SmokeTestFor(typeof(AdminPage))]
    public async Task Ladmin_can_render_administration_and_repair_panel()
    {
        await using var context = new PageRenderContext();
        context.AddAdminServices();
        var page = AssertPage<AdminPage>(context, "Sandbox Management", "Ladmin");
        page.WaitForAssertion(() =>
        {
            Assert.NotNull(page.FindComponent<AdminRepairPanel>());
            Assert.Contains("No sandboxes found.", page.Markup);
        });
    }

    [Fact, SmokeTestFor(typeof(SessionDiagnostics))]
    public async Task User_can_render_current_session_diagnostics()
    {
        await using var context = new PageRenderContext();
        context.AddDiagnosticsServices();
        var page = AssertPage<SessionDiagnostics>(context, "Session Diagnostics", "User");
        page.WaitForAssertion(() => Assert.Contains("Session active", page.Markup));
    }

    [Fact, SmokeTestFor(typeof(DemoStorePage))]
    public async Task User_can_render_store_without_demo_role_grants()
    {
        await using var context = new PageRenderContext();
        context.AddDemoStoreServices();
        await context.SeedSandboxAsync(owner: false);
        var page = AssertPage<DemoStorePage>(context, "Demo Store", "User");
        page.WaitForAssertion(() => Assert.Contains("Your Demo Access", page.Markup));
    }

    [Fact, SmokeTestFor(typeof(AuthorizationLabPage))]
    public async Task User_can_render_authorization_lab_index()
    {
        await using var context = new PageRenderContext();
        AssertPage<AuthorizationLabPage>(context, "Authorization Lab", "User");
    }

    [Fact, SmokeTestFor(typeof(Error))]
    public async Task Error_page_renders_without_a_request_context()
    {
        await using var context = new PageRenderContext();
        AssertPage<Error>(context, "Error.");
    }

    [Fact, SmokeTestFor(typeof(PublicPage))]
    public async Task Public_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<PublicPage>(context, "Public Access");
    }

    [Fact, SmokeTestFor(typeof(AuthenticatedPage))]
    public async Task Authenticated_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<AuthenticatedPage>(context, "Authenticated Page", "User");
    }

    [Fact, SmokeTestFor(typeof(AdminRolePage))]
    public async Task Admin_role_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<AdminRolePage>(context, "Admin Role", "Admin");
    }

    [Fact, SmokeTestFor(typeof(AdminPermissionPage))]
    public async Task Admin_permission_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<AdminPermissionPage>(context, "Admin Permission", "Admin", adminPermission: true);
    }

    [Fact, SmokeTestFor(typeof(AspNetAuthorizePage))]
    public async Task Aspnet_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<AspNetAuthorizePage>(context, "ASP.NET Core Authorize", "User");
    }

    [Fact, SmokeTestFor(typeof(CombinedAuthorizationPage))]
    public async Task Combined_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<CombinedAuthorizationPage>(context, "Combined Authorization", "Admin", adminPermission: true);
    }

    [Fact, SmokeTestFor(typeof(StateViewPage))]
    public async Task State_view_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<StateViewPage>(context, "UAuthStateView", "Admin", authorizationExamples: true);
    }

    [Fact, SmokeTestFor(typeof(MatchModesPage))]
    public async Task Match_modes_authorization_sample_renders()
    {
        await using var context = new PageRenderContext();
        AssertPage<MatchModesPage>(context, "Authorization Match Modes", "Admin", authorizationExamples: true);
    }

    private static IRenderedComponent<T> AssertPage<T>(PageRenderContext context, string expected, string? role = null, bool authorizationExamples = false, bool adminPermission = false) where T : IComponent
    {
        var page = context.RenderPage<T>(role is null ? null : PageRenderContext.Authenticated(role, adminPermission ? new[] { UAuthActions.Users.QueryAdmin } : Array.Empty<string>()));
        page.WaitForAssertion(() =>
        {
            Assert.Contains(expected, page.Markup);
            // A heading must not mask initialization falling into an error fallback.
            // These two sample pages deliberately show denied authorization examples.
            if (!authorizationExamples)
                Assert.Empty(page.FindAll(".mud-alert-error"));
        });
        return page;
    }
}
