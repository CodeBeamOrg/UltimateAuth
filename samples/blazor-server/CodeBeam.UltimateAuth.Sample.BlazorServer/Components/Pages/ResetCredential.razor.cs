using CodeBeam.UltimateAuth.Credentials.Contracts;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class ResetCredential
{
    private MudForm _form = null!;
    private string? _code;
    private string? _newPassword;
    private string? _newPasswordCheck;

    private async Task ResetPasswordAsync()
    {
        await _form.ValidateAsync();
        if (!_form.IsValid)
        {
            Snackbar.Add("Please fix the validation errors.", Severity.Error);
            return;
        }

        if (_newPassword != _newPasswordCheck)
        {
            Snackbar.Add("Passwords do not match.", Severity.Error);
            return;
        }

        if (string.IsNullOrEmpty(_code) || string.IsNullOrEmpty(_newPassword) || string.IsNullOrEmpty(Identifier))
        {
            Snackbar.Add("Missing required information. Please ensure you have a valid reset code and new password.", Severity.Error);
            return;
        }

        var request = new CompleteResetCredentialRequest
        {
            ResetToken = _code,
            NewSecret = _newPassword,
            Identifier = Identifier // Coming from UAuthFlowPageBase automatically if begin reset is successful
        };

        var result = await UAuthClient.Credentials.CompleteResetMyAsync(request);

        if (result.IsSuccess)
        {
            Snackbar.Add("Credential reset successfully. Please log in with your new password.", Severity.Success);
            Navigation.NavigateTo("/login");
        }
        else
        {
            Snackbar.Add(result.Problem?.Detail ?? result.Problem?.Title ?? "Failed to reset credential. Please try again.", Severity.Error);
        }
    }

    private string PasswordMatch(string arg) => _newPassword != arg ? "Passwords don't match" : string.Empty;
}
