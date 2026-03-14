using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Pages;

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
