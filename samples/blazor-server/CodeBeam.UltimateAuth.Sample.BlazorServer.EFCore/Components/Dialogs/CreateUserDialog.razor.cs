using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Components.Dialogs;

public partial class CreateUserDialog
{
    private MudForm _form = null!;
    private string? _username;
    private string? _email;
    private string? _password;
    private string? _passwordCheck;
    private string? _displayName;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private async Task CreateUserAsync()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        if (_password != _passwordCheck)
        {
            Snackbar.Add("Passwords don't match.", Severity.Error);
            return;
        }

        var request = new CreateUserRequest
        {
            UserName = _username,
            Email = _email,
            DisplayName = _displayName,
            Password = _password
        };

        var result = await UAuthClient.Users.CreateAsAdminAsync(request);

        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorText ?? "User creation failed.", Severity.Error);
            return;
        }

        Snackbar.Add("User created successfully", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private string PasswordMatch(string? arg) => _password != arg ? "Passwords don't match." : string.Empty;

    private void Cancel() => MudDialog.Cancel();
}
