using CodeBeam.UltimateAuth.Client;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Custom;

public partial class NotAuthorized
{
    [Inject] private IUAuthClient UAuthClient { get; set; } = default!;
    [Inject] private IUAuthStateManager StateManager { get; set; } = default!;

    private bool _busy;
    private string? _diagnostic;

    private async Task ValidateAsync()
    {
        _busy = true;

        try
        {
            var result = await UAuthClient.Flows.ValidateAsync();

            _diagnostic = $"Validate: IsValid={result.IsValid}, State={result.State}";
        }
        catch (Exception ex)
        {
            _diagnostic = $"Validate error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _busy = false;
        }

        //await StateManager.EnsureAsync(force: true);

        //Console.WriteLine(
        //    $"Authenticated: {StateManager.State.IsAuthenticated}");

        //Console.WriteLine(
        //    $"Identity: {StateManager.State.Identity?.PrimaryUserName}");
    }

    private async Task RefreshAsync()
    {
        _busy = true;

        try
        {
            var result = await UAuthClient.Flows.RefreshAsync();

            _diagnostic =
                $"Refresh: Success={result.IsSuccess}, " +
                $"Status={result.Status}, Outcome={result.Outcome}";
        }
        catch (Exception ex)
        {
            _diagnostic = $"Refresh error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _busy = false;
        }
    }

    private string LoginHref
    {
        get
        {
            var returnUrl = Uri.EscapeDataString(Nav.ToBaseRelativePath(Nav.Uri));
            return $"/login?returnUrl=/{returnUrl}";
        }
    }

    private void GoBack() => Nav.NavigateTo("/", replace: false);
}
