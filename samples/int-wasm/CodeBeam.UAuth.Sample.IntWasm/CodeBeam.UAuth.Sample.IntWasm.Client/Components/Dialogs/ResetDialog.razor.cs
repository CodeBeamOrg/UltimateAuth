using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UAuth.Sample.IntWasm.Client.Components.Dialogs;

public partial class ResetDialog
{
    private bool _resetRequested = false;
    private string? _resetCode;
    private string? _identifier;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    private async Task RequestResetAsync()
    {
        var request = new BeginResetCredentialRequest
        {
            CredentialType = CredentialType.Password,
            ResetCodeType = ResetCodeType.Code,
            Identifier = _identifier ?? string.Empty
        };

        var result = await UAuthClient.Credentials.BeginResetMyAsync(request);
        if (!result.IsSuccess || result.Value is null)
        {
            Snackbar.Add(result.ErrorText ?? "Failed to request credential reset.", Severity.Error);
            return;
        }

        _resetCode = result.Value.Token;
        _resetRequested = true;
    }

    private void Cancel() => MudDialog.Cancel();
}
