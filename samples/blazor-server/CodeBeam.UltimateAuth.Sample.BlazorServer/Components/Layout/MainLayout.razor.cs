using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Layout;

public partial class MainLayout
{
    [CascadingParameter]
    public UAuthState UAuth { get; set; } = default!;

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
}
