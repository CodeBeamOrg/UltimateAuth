using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class SessionDialog
{
    private MudDataGrid<SessionChainSummary>? _grid;
    private SessionChainDetail? _chainDetail;

    private bool _loading;
    private bool _reloadQueued;
    private const int SessionPageSize = 10;
    private int _sessionPage = 1;

    private readonly List<PreviewDevice> _previewDevices =
    [
        new(
            "Windows Desktop",
            "Windows",
            "Edge",
            "Just now",
            true,
            Icons.Material.Filled.Computer),
        new(
            "Mobile Device",
            "Android",
            "Chrome",
            "2 hours ago",
            false,
            Icons.Material.Filled.Smartphone),
        new(
            "MacBook",
            "macOS",
            "Safari",
            "Yesterday",
            false,
            Icons.Material.Filled.LaptopMac)
    ];

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey? UserKey { get; set; }

    [Parameter]
    public SessionChainId? InitialChainId { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!PreviewMode && InitialChainId is { } chainId)
        {
            await ShowChainDetailsAsync(chainId);
        }
    }

    private async Task<GridData<SessionChainSummary>> LoadServerData(GridState<SessionChainSummary> state, CancellationToken ct)
    {
        if (PreviewMode)
            return EmptyGrid();

        var sort = state.SortDefinitions?.FirstOrDefault();

        var request = new PageRequest
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        try
        {
            UAuthResult<PagedResult<SessionChainSummary>> result =
                UserKey is null
                    ? await UAuthClient.Sessions.GetMyChainsAsync(request)
                    : await UAuthClient.Sessions.GetUserChainsAsync(
                        UserKey.Value, request);

            if (!result.IsSuccess || result.Value is null)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to load sessions.",
                    Severity.Error);
                return EmptyGrid();
            }

            return new GridData<SessionChainSummary>
            {
                Items = result.Value.Items,
                TotalItems = result.Value.TotalCount
            };
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to load sessions.", Severity.Error);
            return EmptyGrid();
        }
    }

    private static GridData<SessionChainSummary> EmptyGrid() => new()
    {
        Items = Array.Empty<SessionChainSummary>(),
        TotalItems = 0
    };

    private async Task ReloadAsync()
    {
        if (PreviewMode || _grid is null)
            return;

        if (_loading)
        {
            _reloadQueued = true;
            return;
        }

        _loading = true;

        try
        {
            await _grid.ReloadServerData();
        }
        finally
        {
            _loading = false;
        }

        if (_reloadQueued)
        {
            _reloadQueued = false;
            await ReloadAsync();
        }
    }

    private async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText)
    {
        if (PreviewMode || _loading)
            return false;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            title,
            message,
            yesText: confirmText,
            noText: "Cancel");

        return confirmed == true;
    }

    private async Task LogoutAllAsync()
    {
        if (!await ConfirmAsync(
            "Logout All Devices",
            "All devices will be signed out, including the current device.",
            "Logout All"))
            return;

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Flows.LogoutAllMyDevicesAsync()
                : UAuthClient.Flows.LogoutAllUserDevicesAsync(UserKey.Value),
            "All devices logged out.",
            navigateOnSuccess: UserKey is null);
    }

    private async Task LogoutOthersAsync()
    {
        if (!await ConfirmAsync(
            "Logout Other Devices",
            "Other devices will be signed out. Your current device will remain signed in.",
            "Logout Others"))
            return;

        await ExecuteAsync(
            () => UAuthClient.Flows.LogoutMyOtherDevicesAsync(),
            "Other devices logged out.");
    }

    private async Task RevokeAllAsync()
    {
        if (!await ConfirmAsync(
            "Revoke All Devices",
            "All device chains will be revoked. This action may terminate your current session.",
            "Revoke All"))
            return;

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Sessions.RevokeAllMyChainsAsync()
                : UAuthClient.Sessions.RevokeAllUserChainsAsync(UserKey.Value),
            "All device chains revoked.",
            navigateOnSuccess: UserKey is null);
    }

    private async Task RevokeOthersAsync()
    {
        if (!await ConfirmAsync(
            "Revoke Other Devices",
            "All device chains except the current one will be revoked.",
            "Revoke Others"))
            return;

        await ExecuteAsync(
            () => UAuthClient.Sessions.RevokeMyOtherChainsAsync(),
            "Other device chains revoked.");
    }

    private async Task LogoutDeviceAsync(SessionChainId chainId)
    {
        if (!await ConfirmAsync(
            "Logout Device",
            "Sign out the selected device?",
            "Logout"))
            return;

        var request = new LogoutDeviceRequest { ChainId = chainId };

        await ExecuteRevokeAsync(
            () => UserKey is null
                ? UAuthClient.Flows.LogoutMyDeviceAsync(request)
                : UAuthClient.Flows.LogoutUserDeviceAsync(
                    UserKey.Value, request),
            "Device logged out.");
    }

    private async Task RevokeChainAsync(SessionChainId chainId)
    {
        if (!await ConfirmAsync(
            "Revoke Device",
            "Revoke this device chain and invalidate its access?",
            "Revoke"))
            return;

        await ExecuteRevokeAsync(
            () => UserKey is null
                ? UAuthClient.Sessions.RevokeMyChainAsync(chainId)
                : UAuthClient.Sessions.RevokeUserChainAsync(
                    UserKey.Value, chainId),
            "Device revoked.");
    }

    private async Task ExecuteAsync(
        Func<Task<UAuthResult>> operation,
        string successMessage,
        bool navigateOnSuccess = false)
    {
        if (PreviewMode || _loading)
            return;

        _loading = true;
        var succeeded = false;

        try
        {
            var result = await operation();

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Session operation failed.",
                    Severity.Error);
                return;
            }

            succeeded = true;
            Snackbar.Add(successMessage, Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Session operation failed.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }

        if (!succeeded)
            return;

        if (navigateOnSuccess)
        {
            Nav.NavigateTo("/login");
            return;
        }

        await ReloadAsync();
    }

    private async Task ExecuteRevokeAsync(
        Func<Task<UAuthResult<RevokeResult>>> operation,
        string successMessage)
    {
        if (PreviewMode || _loading)
            return;

        _loading = true;
        bool currentChain = false;
        bool succeeded = false;

        try
        {
            var result = await operation();

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Session operation failed.",
                    Severity.Error);
                return;
            }

            succeeded = true;
            currentChain = result.Value?.CurrentChain == true;

            Snackbar.Add(successMessage, Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Session operation failed.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }

        if (!succeeded)
            return;

        if (currentChain)
        {
            Nav.NavigateTo("/login");
            return;
        }

        _chainDetail = null;
        await ReloadAsync();
    }

    private async Task ShowChainDetailsAsync(SessionChainId chainId)
    {
        if (PreviewMode || _loading)
            return;

        _loading = true;

        try
        {
            UAuthResult<SessionChainDetail> result =
                UserKey is null
                    ? await UAuthClient.Sessions.GetMyChainDetailAsync(chainId)
                    : await UAuthClient.Sessions.GetUserChainDetailAsync(
                        UserKey.Value, chainId);

            if (!result.IsSuccess || result.Value is null)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to load device details.",
                    Severity.Error);
                return;
            }

            _chainDetail = result.Value;
            _sessionPage = 1;
        }
        catch (Exception)
        {
            Snackbar.Add("Failed to load device details.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void ClearDetail() => _chainDetail = null;

    private void Cancel()
    {
        if (!_loading)
            MudDialog.Cancel();
    }

    private sealed record PreviewDevice(
        string Name,
        string Platform,
        string Browser,
        string LastSeen,
        bool IsCurrent,
        string Icon);
}
