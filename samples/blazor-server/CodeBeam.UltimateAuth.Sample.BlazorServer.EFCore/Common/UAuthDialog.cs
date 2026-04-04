using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Common;

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

    public static DialogOptions GetDialogOptions(MaxWidth maxWidth = MaxWidth.Medium)
    {
        return new DialogOptions
        {
            MaxWidth = maxWidth,
            FullWidth = true,
            CloseButton = true
        };
    }
}
