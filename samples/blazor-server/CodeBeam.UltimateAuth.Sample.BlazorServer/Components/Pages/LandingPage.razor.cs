using CodeBeam.UltimateAuth.Core.Constants;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class LandingPage
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var state = await AuthProvider.GetAuthenticationStateAsync();
        var isAuthenticated = state.User.Identity?.IsAuthenticated == true;

        Nav.NavigateTo(isAuthenticated ? "/home" : $"{UAuthConstants.Routes.LoginRedirect}?fresh=true");
    }
}
