using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Page;

public partial class SampleLandingPage
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
