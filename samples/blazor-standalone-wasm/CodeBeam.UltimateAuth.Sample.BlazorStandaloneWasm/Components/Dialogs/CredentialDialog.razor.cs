using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class CredentialDialog
{
    private MudForm _form = null!;
    private string? _oldPassword;
    private string? _newPassword;
    private string? _newPasswordCheck;
    private bool _passwordMode1 = false;
    private bool _passwordMode2 = false;
    private bool _passwordMode3 = true;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    [Parameter]
    public UserKey? UserKey { get; set; }

    private async Task ChangePasswordAsync()
    {
        if (_form is null)
            return;

        await _form.Validate();
        if (!_form.IsValid)
        {
            Snackbar.Add("Form is not valid.", Severity.Error);
            return;
        }

        if (_newPassword != _newPasswordCheck)
        {
            Snackbar.Add("New password and check do not match", Severity.Error);
            return;
        }

        ChangeCredentialRequest request;

        if (UserKey is null)
        {
            request = new ChangeCredentialRequest
            {
                CurrentSecret = _oldPassword!,
                NewSecret = _newPassword!
            };
        }
        else
        {
            request = new ChangeCredentialRequest
            {
                NewSecret = _newPassword!
            };
        }

        UAuthResult<ChangeCredentialResult> result;
        if (UserKey is null)
        {
            result = await UAuthClient.Credentials.ChangeMyAsync(request);
        }
        else
        {
            result = await UAuthClient.Credentials.ChangeUserAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Password changed successfully", Severity.Success);
            _oldPassword = null;
            _newPassword = null;
            _newPasswordCheck = null;
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "An error occurred while changing password", Severity.Error);
        }
    }

    private string PasswordMatch(string arg) => _newPassword != arg ? "Passwords don't match" : string.Empty;

    private void Cancel() => MudDialog.Cancel();
}
