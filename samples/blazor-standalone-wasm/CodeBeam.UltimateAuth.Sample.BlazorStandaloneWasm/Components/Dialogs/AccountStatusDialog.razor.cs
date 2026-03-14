using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class AccountStatusDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    private async Task SuspendAccountAsync()
    {
        var info = await DialogService.ShowMessageBoxAsync(
            title: "Are You Sure",
            markupMessage: (MarkupString)
                """
            You are going to suspend your account.<br/><br/>
            You can still active your account later.
            """,
            yesText: "Suspend", noText: "Cancel",
            options: new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "uauth-blur-slight" });

        if (info != true)
        {
            Snackbar.Add("Suspend process cancelled.", Severity.Info);
            return;
        }

        ChangeUserStatusSelfRequest request = new() { NewStatus = SelfUserStatus.SelfSuspended };
        var result = await UAuthClient.Users.ChangeStatusSelfAsync(request);
        if (result.IsSuccess)
        {
            Snackbar.Add("Your account suspended successfully.", Severity.Success);
            MudDialog.Close();
        }
        else
        {
            Snackbar.Add(result?.GetErrorText ?? "Delete failed.", Severity.Error);
        }
    }

    private async Task DeleteAccountAsync()
    {
        var info = await DialogService.ShowMessageBoxAsync(
            title: "Are You Sure",
            markupMessage: (MarkupString)
                """
            You are going to delete your account.<br/><br/>
            This action can't be undone.<br/><br/>
            (Actually it is, admin can handle soft deleted accounts.)
            """,
            yesText: "Delete", noText: "Cancel",
            options: new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "uauth-blur-slight" });

        if (info != true)
        {
            Snackbar.Add("Deletion cancelled.", Severity.Info);
            return;
        }

        var result = await UAuthClient.Users.DeleteMeAsync();
        if (result.IsSuccess)
        {
            Snackbar.Add("Your account deleted successfully.", Severity.Success);
            MudDialog.Close();
        }
        else
        {
            Snackbar.Add(result?.GetErrorText ?? "Delete failed.", Severity.Error);
        }
    }
}
