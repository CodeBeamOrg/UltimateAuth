using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class CredentialDialog
{
    private MudForm _form = null!;

    private string? _oldPassword;
    private string? _newPassword;
    private string? _newPasswordCheck;

    private bool _isSubmitting;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey? UserKey { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private async Task ChangePasswordAsync()
    {
        if (PreviewMode || _isSubmitting || _form is null)
            return;

        await _form.ValidateAsync();

        if (!_form.IsValid)
            return;

        if (_newPassword != _newPasswordCheck)
        {
            Snackbar.Add("Passwords don't match.", Severity.Error);
            return;
        }

        var request = new ChangeCredentialRequest
        {
            CurrentSecret = UserKey is null ? _oldPassword : null,
            NewSecret = _newPassword!
        };

        _isSubmitting = true;

        try
        {
            UAuthResult<CredentialChangeResult> result =
                UserKey is null
                    ? await UAuthClient.Credentials.ChangeMyAsync(request)
                    : await UAuthClient.Credentials.ChangeUserAsync(
                        UserKey.Value, request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to update password.",
                    Severity.Error);
                return;
            }

            _oldPassword = null;
            _newPassword = null;
            _newPasswordCheck = null;

            Snackbar.Add(
                UserKey is null
                    ? "Password changed successfully."
                    : "User password updated successfully.",
                Severity.Success);

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred while updating the password.",
                Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private string? PasswordMatch(string? value)
    {
        return _newPassword == value
            ? null
            : "Passwords don't match.";
    }

    private void Cancel()
    {
        if (!_isSubmitting)
            MudDialog.Cancel();
    }
}
