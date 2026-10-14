namespace CodeBeam.UltimateAuth.Sample.Shared.Components.PageComponents;

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
