using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Sample.BlazorServer.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Layout;

public partial class MainLayout
{
    [CascadingParameter]
    public UAuthState UAuth { get; set; } = default!;

    [CascadingParameter]
    public DarkModeManager DarkModeManager { get; set; } = default!;

    private async Task Refresh()
    {
        await UAuthClient.Flows.RefreshAsync();
    }

    private async Task Logout()
    {
        await UAuthClient.Flows.LogoutAsync();
    }

    private Color GetBadgeColor()
    {
        if (UAuth is null || !UAuth.IsAuthenticated)
            return Color.Error;

        if (UAuth.IsStale)
            return Color.Warning;

        var state = UAuth.Identity?.SessionState;

        if (state is null || state == SessionState.Active)
            return Color.Success;

        if (state == SessionState.Invalid)
            return Color.Error;

        return Color.Warning;
    }

    private void HandleSignInClick()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);

        if (uri.AbsolutePath.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            Nav.NavigateTo("/login?focus=1", replace: true, forceLoad: true);
            return;
        }

        GoToLoginWithReturn();
    }

    private void GoToLoginWithReturn()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);

        if (uri.AbsolutePath.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            Nav.NavigateTo("/login", replace: true);
            return;
        }

        var current = Nav.ToBaseRelativePath(uri.ToString());
        if (string.IsNullOrWhiteSpace(current))
            current = "home";

        var returnUrl = Uri.EscapeDataString("/" + current.TrimStart('/'));
        Nav.NavigateTo($"/login?returnUrl={returnUrl}", replace: true);
    }
}
