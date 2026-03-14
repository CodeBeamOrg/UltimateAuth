using CodeBeam.UltimateAuth.Credentials.Contracts;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Pages;

public partial class ResetCredential
{
    private MudForm _form = null!;
    private string? _code;
    private string? _newPassword;
    private string? _newPasswordCheck;

    private async Task ResetPasswordAsync()
    {
        await _form.Validate();
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

        var request = new CompleteCredentialResetRequest
        {
            ResetToken = _code,
            NewSecret = _newPassword ?? string.Empty,
            Identifier = Identifier // Coming from UAuthFlowPageBase automatically if begin reset is successful
        };

        var result = await UAuthClient.Credentials.CompleteResetMyAsync(request);

        if (result.IsSuccess)
        {
            Snackbar.Add("Credential reset successfully. Please log in with your new password.", Severity.Success);
            Nav.NavigateTo("/login");
        }
        else
        {
            Snackbar.Add(result.Problem?.Detail ?? result.Problem?.Title ?? "Failed to reset credential. Please try again.", Severity.Error);
        }
    }

    private string PasswordMatch(string arg) => _newPassword != arg ? "Passwords don't match" : string.Empty;
}
