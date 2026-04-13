namespace CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore.Components.Pages;

public partial class NotAuthorized
{
    private string LoginHref
    {
        get
        {
            var returnUrl = Uri.EscapeDataString(Nav.ToBaseRelativePath(Nav.Uri));
            return $"/login?returnUrl=/{returnUrl}";
        }
    }

    private void GoBack() => Nav.NavigateTo("/", replace: false);
}
