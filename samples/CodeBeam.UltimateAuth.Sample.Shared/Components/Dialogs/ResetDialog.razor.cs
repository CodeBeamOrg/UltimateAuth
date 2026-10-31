using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class ResetDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private bool _resetRequested;
    private bool _isSubmitting;
    private string? _identifier;

    private async Task RequestResetAsync()
    {
        if (_isSubmitting || string.IsNullOrWhiteSpace(_identifier))
            return;

        if (PreviewMode)
        {
            _resetRequested = true;
            return;
        }

        _isSubmitting = true;

        try
        {
            var request = new BeginResetCredentialRequest
            {
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Code,
                Identifier = _identifier.Trim()
            };

            var result = await UAuthClient.Credentials
                .BeginResetAsync(request);

            if (!result.IsSuccess || result.Value is null)
            {
                Snackbar.Add(
                    result.ErrorText ??
                    "Failed to request password reset.",
                    Severity.Error);
                return;
            }

            _resetRequested = true;
        }
        catch (Exception)
        {
            Snackbar.Add(
                "Unable to request password reset.",
                Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
            return RequestResetAsync();

        return Task.CompletedTask;
    }

    private Task ContinueResetAsync()
    {
        if (PreviewMode)
        {
            Snackbar.Add(
                "Navigation is disabled in preview mode.",
                Severity.Info);
            return Task.CompletedTask;
        }

        if (!_resetRequested || string.IsNullOrWhiteSpace(_identifier))
            return Task.CompletedTask;

        var encodedIdentifier = Uri.EscapeDataString(_identifier.Trim());

        MudDialog.Close();

        Nav.NavigateTo(
            $"/reset?uauth_identifier={encodedIdentifier}");

        return Task.CompletedTask;
    }

    private void StartOver()
    {
        if (_isSubmitting)
            return;

        _resetRequested = false;
        _identifier = null;
    }

    private void Cancel()
    {
        if (!_isSubmitting)
            MudDialog.Cancel();
    }
}
