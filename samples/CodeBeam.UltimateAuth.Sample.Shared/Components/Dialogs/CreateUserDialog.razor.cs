
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Mail;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class CreateUserDialog
{
    private MudForm _form = null!;

    private string? _username;
    private string? _email;
    private string? _password;
    private string? _passwordCheck;
    private string? _displayName;

    private bool _isSubmitting;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    /// <summary>
    /// Enables form preview without submitting a user creation request.
    /// </summary>
    [Parameter]
    public bool PreviewMode { get; set; }

    /// <summary>
    /// Optional custom user creation handler.
    /// When supplied, the default UltimateAuth admin operation is bypassed.
    /// The caller must enforce authorization and application-specific rules.
    /// </summary>
    [Parameter]
    public Func<CreateUserRequest, Task<CreateUserDialogResult>>?
        OnCreateUserAsync
    { get; set; }

    private async Task CreateUserAsync()
    {
        if (_isSubmitting || PreviewMode)
            return;

        await _form.ValidateAsync();

        if (!_form.IsValid)
            return;

        if (_password != _passwordCheck)
        {
            Snackbar.Add("Passwords don't match.", Severity.Error);
            return;
        }

        var request = new CreateUserRequest
        {
            UserName = _username?.Trim(),
            Email = _email?.Trim(),
            DisplayName = _displayName?.Trim(),
            Password = _password
        };

        _isSubmitting = true;

        try
        {
            CreateUserDialogResult outcome;

            if (OnCreateUserAsync is not null)
            {
                outcome = await OnCreateUserAsync(request);
            }
            else
            {
                var result = await UAuthClient.Users.CreateAsAdminAsync(request);

                outcome = result.IsSuccess
                    ? CreateUserDialogResult.Success()
                    : CreateUserDialogResult.Failure(
                        result.ErrorText ?? "User creation failed.");
            }

            if (!outcome.IsSuccess)
            {
                Snackbar.Add(
                    outcome.ErrorMessage ?? "User creation failed.",
                    Severity.Error);

                return;
            }

            Snackbar.Add("User created successfully.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred while creating the user.",
                Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private string? PasswordMatch(string? value)
    {
        return _password == value
            ? null
            : "Passwords don't match.";
    }

    private static string? ValidateEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Email is required.";

        try
        {
            var address = new MailAddress(value);

            return string.Equals(
                address.Address,
                value.Trim(),
                StringComparison.OrdinalIgnoreCase)
                ? null
                : "Enter a valid email address.";
        }
        catch (FormatException)
        {
            return "Enter a valid email address.";
        }
    }

    private void Cancel()
    {
        if (!_isSubmitting)
            MudDialog.Cancel();
    }
}
