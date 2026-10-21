using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class AccountStatusDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private bool _isSubmitting;

    private string AccountName =>
        PreviewMode
            ? "Alex Morgan"
            : AuthState?.Identity?.DisplayName ?? "Your Account";

    private async Task SuspendAccountAsync()
    {
        if (PreviewMode || _isSubmitting)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            title: "Suspend Account",
            message:
                "Are you sure you want to suspend your account? " +
                "You may lose access until your account is reactivated.",
            yesText: "Suspend Account",
            noText: "Cancel",
            options: new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackgroundClass = "uauth-blur-slight"
            });

        if (confirmed != true || _isSubmitting)
            return;

        _isSubmitting = true;

        try
        {
            var request = new ChangeUserStatusSelfRequest
            {
                NewStatus = SelfAssignableUserStatus.SelfSuspended
            };

            var result = await UAuthClient.Users
                .ChangeMyStatusAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to suspend account.",
                    Severity.Error);
                return;
            }

            Snackbar.Add(
                "Your account has been suspended.",
                Severity.Success);

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred while suspending your account.",
                Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task DeleteAccountAsync()
    {
        if (PreviewMode || _isSubmitting)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            title: "Delete Account",
            message:
                "Are you sure you want to delete your account? " +
                "You will lose access to this identity. " +
                "Recovery may not be available.",
            yesText: "Continue",
            noText: "Cancel",
            options: new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackgroundClass = "uauth-blur-slight"
            });

        if (confirmed != true || _isSubmitting)
            return;

        // Require an additional confirmation for account deletion.
        var finalConfirmation = await DialogService.ShowMessageBoxAsync(
            title: "Confirm Account Deletion",
            message:
                "This action will delete your account. " +
                "Do you want to proceed?",
            yesText: "Delete My Account",
            noText: "Cancel",
            options: new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackgroundClass = "uauth-blur-slight"
            });

        if (finalConfirmation != true || _isSubmitting)
            return;

        _isSubmitting = true;

        try
        {
            var result = await UAuthClient.Users.DeleteMeAsync();

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to delete account.",
                    Severity.Error);
                return;
            }

            Snackbar.Add(
                "Your account has been deleted.",
                Severity.Success);

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred while deleting your account.",
                Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Close()
    {
        if (!_isSubmitting)
            MudDialog.Close();
    }
}
