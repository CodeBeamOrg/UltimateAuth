using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Common;

public static class UAuthDialog
{
    public static DialogParameters GetDialogParameters(UAuthState state, UserKey? userKey = null)
    {
        DialogParameters parameters = new DialogParameters();
        parameters.Add("AuthState", state);
        if (userKey != null )
        {
            parameters.Add("UserKey", userKey);
        }
        return parameters;
    }

    public static DialogOptions GetDialogOptions(
        MaxWidth maxWidth = MaxWidth.Medium,
        bool allowBackdropClose = false,
        bool allowEscapeClose = false,
        bool showCloseButton = true)
    {
        return new DialogOptions
        {
            MaxWidth = maxWidth,
            FullWidth = true,
            BackdropClick = allowBackdropClose,
            CloseOnEscapeKey = allowEscapeClose,
            CloseButton = showCloseButton,
            BackgroundClass = "uauth-dialog-backdrop",
        };
    }
}
