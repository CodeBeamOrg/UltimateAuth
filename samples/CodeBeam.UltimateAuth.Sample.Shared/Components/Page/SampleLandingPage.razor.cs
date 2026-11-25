using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Page;

public partial class SampleLandingPage
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var authState = await AuthenticationStateTask;
        var isAuthenticated = authState.User.Identity?.IsAuthenticated == true;

        Nav.NavigateTo(isAuthenticated ? "/home" : $"{UAuthConstants.Routes.LoginRedirect}?fresh=true");
    }
}
