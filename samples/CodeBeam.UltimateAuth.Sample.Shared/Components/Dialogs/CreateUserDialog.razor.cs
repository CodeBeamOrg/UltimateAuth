using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Mail;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class CreateUserDialog : IDisposable
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

        if (!IdentifiersAvailable)
        {
            Snackbar.Add("Please wait until username and email availability checks are complete.", Severity.Warning);
            return;
        }

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


    private IdentifierAvailabilityState _usernameAvailability =
        IdentifierAvailabilityState.Empty;

    private IdentifierAvailabilityState _emailAvailability =
        IdentifierAvailabilityState.Empty;

    private CancellationTokenSource? _usernameCheckCts;
    private CancellationTokenSource? _emailCheckCts;

    private int _usernameVersion;
    private int _emailVersion;

    private bool _disposed;

    private bool IdentifiersAvailable =>
        _usernameAvailability == IdentifierAvailabilityState.Available &&
        _emailAvailability == IdentifierAvailabilityState.Available;

    private Task OnUsernameChanged()
    {
        _usernameCheckCts?.Cancel();

        var version = ++_usernameVersion;

        _usernameAvailability = string.IsNullOrWhiteSpace(_username)
            ? IdentifierAvailabilityState.Empty
            : IdentifierAvailabilityState.Checking;

        if (string.IsNullOrWhiteSpace(_username))
            return Task.CompletedTask;

        var cts = new CancellationTokenSource();
        _usernameCheckCts = cts;

        _ = CheckIdentifierAsync(
            UserIdentifierType.Username,
            _username.Trim(),
            version,
            cts);

        return Task.CompletedTask;
    }

    private Task OnEmailChanged()
    {
        _emailCheckCts?.Cancel();

        var version = ++_emailVersion;

        _emailAvailability = string.IsNullOrWhiteSpace(_email)
            ? IdentifierAvailabilityState.Empty
            : IdentifierAvailabilityState.Checking;

        if (string.IsNullOrWhiteSpace(_email))
            return Task.CompletedTask;

        if (ValidateEmail(_email) is not null)
        {
            _emailAvailability = IdentifierAvailabilityState.Invalid;
            return Task.CompletedTask;
        }

        var cts = new CancellationTokenSource();
        _emailCheckCts = cts;

        _ = CheckIdentifierAsync(
            UserIdentifierType.Email,
            _email.Trim(),
            version,
            cts);

        return Task.CompletedTask;
    }

    private async Task CheckIdentifierAsync(
        UserIdentifierType type,
        string value,
        int version,
        CancellationTokenSource cts)
    {
        try
        {
            var token = cts.Token;

            await Task.Delay(450, token);

            if (token.IsCancellationRequested || _disposed)
                return;

            var result = await UAuthClient.Identifiers.CheckAvailabilityAsync(
                new CheckUserIdentifierAvailabilityRequest
                {
                    Type = type,
                    Value = value
                });

            if (token.IsCancellationRequested || _disposed)
                return;

            var state = !result.IsSuccess || result.Value is null
                ? IdentifierAvailabilityState.Error
                : !result.Value.IsValid
                    ? IdentifierAvailabilityState.Invalid
                    : result.Value.IsAvailable
                        ? IdentifierAvailabilityState.Available
                        : IdentifierAvailabilityState.Unavailable;

            await InvokeAsync(() =>
            {
                if (_disposed || token.IsCancellationRequested)
                    return;

                if (type == UserIdentifierType.Username)
                {
                    if (version != _usernameVersion)
                        return;

                    _usernameAvailability = state;
                }
                else
                {
                    if (version != _emailVersion)
                        return;

                    _emailAvailability = state;
                }

                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // A newer input superseded this check.
        }
        catch (Exception)
        {
            if (!_disposed && !cts.IsCancellationRequested)
            {
                await InvokeAsync(() =>
                {
                    if (_disposed)
                        return;

                    if (type == UserIdentifierType.Username &&
                        version == _usernameVersion)
                    {
                        _usernameAvailability = IdentifierAvailabilityState.Error;
                    }
                    else if (type == UserIdentifierType.Email &&
                             version == _emailVersion)
                    {
                        _emailAvailability = IdentifierAvailabilityState.Error;
                    }

                    StateHasChanged();
                });
            }
        }
        finally
        {
            // The asynchronous operation owns its CTS.
            // Dispose only after the operation has finished.
            if (type == UserIdentifierType.Username)
            {
                if (ReferenceEquals(_usernameCheckCts, cts))
                    _usernameCheckCts = null;
            }
            else
            {
                if (ReferenceEquals(_emailCheckCts, cts))
                    _emailCheckCts = null;
            }

            cts.Dispose();
        }
    }

    public void Dispose()
    {
        _disposed = true;

        _usernameCheckCts?.Cancel();
        _emailCheckCts?.Cancel();

        // CTS instances are disposed by their own
        // CheckIdentifierAsync finally blocks.
    }
}
