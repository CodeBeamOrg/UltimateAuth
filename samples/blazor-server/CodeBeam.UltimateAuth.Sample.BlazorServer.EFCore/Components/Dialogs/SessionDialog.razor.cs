using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Components.Dialogs;

public partial class SessionDialog
{
    private MudDataGrid<SessionChainSummary>? _grid;
    private bool _loading = false;
    private bool _reloadQueued;
    private SessionChainDetail? _chainDetail;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    [Parameter]
    public UserKey? UserKey { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            var result = await UAuthClient.Sessions.GetMyChainsAsync();
            if (result != null && result.IsSuccess && result.Value != null)
            {
                await ReloadAsync();
                StateHasChanged();
            }
        }
    }

    private async Task<GridData<SessionChainSummary>> LoadServerData(GridState<SessionChainSummary> state, CancellationToken ct)
    {
        var sort = state.SortDefinitions?.FirstOrDefault();

        var req = new PageRequest
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        UAuthResult<PagedResult<SessionChainSummary>> res;

        if (UserKey is null)
        {
            res = await UAuthClient.Sessions.GetMyChainsAsync(req);
        }
        else
        {
            res = await UAuthClient.Sessions.GetUserChainsAsync(UserKey.Value, req);
        }

        if (!res.IsSuccess || res.Value is null)
        {
            Snackbar.Add(res.Problem?.Title ?? "Failed", Severity.Error);

            return new GridData<SessionChainSummary>
            {
                Items = Array.Empty<SessionChainSummary>(),
                TotalItems = 0
            };
        }

        return new GridData<SessionChainSummary>
        {
            Items = res.Value.Items,
            TotalItems = res.Value.TotalCount
        };
    }

    private async Task ReloadAsync()
    {
        if (_loading)
        {
            _reloadQueued = true;
            return;
        }

        if (_grid is null)
            return;

        _loading = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(300);

        try
        {
            await _grid.ReloadServerData();
        }
        finally
        {
            _loading = false;

            if (_reloadQueued)
            {
                _reloadQueued = false;
                await ReloadAsync();
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LogoutAllAsync()
    {
        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Flows.LogoutAllDevicesSelfAsync();
        }
        else
        {
            result = await UAuthClient.Flows.LogoutAllDevicesAdminAsync(UserKey.Value);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Logged out of all devices.", Severity.Success);
            if (UserKey is null)
                Nav.NavigateTo("/login");
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Failed to logout.", Severity.Error);
        }
    }

    private async Task LogoutOthersAsync()
    {
        var result = await UAuthClient.Flows.LogoutOtherDevicesSelfAsync();

        if (result.IsSuccess)
        {
            Snackbar.Add("Logged out of other devices.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to logout", Severity.Error);
        }
    }

    private async Task LogoutDeviceAsync(SessionChainId chainId)
    {
        LogoutDeviceRequest request = new() { ChainId = chainId };
        UAuthResult<RevokeResult> result;

        if (UserKey is null)
        {
            result = await UAuthClient.Flows.LogoutDeviceSelfAsync(request);
        }
        else
        {
            result = await UAuthClient.Flows.LogoutDeviceAdminAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Logged out of device.", Severity.Success);
            if (result?.Value?.CurrentChain == true)
            {
                Nav.NavigateTo("/login");
                return;
            }
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Failed to logout.", Severity.Error);
        }
    }

    private async Task RevokeAllAsync()
    {
        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Sessions.RevokeAllMyChainsAsync();
        }
        else
        {
            result = await UAuthClient.Sessions.RevokeAllUserChainsAsync(UserKey.Value);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Logged out of all devices.", Severity.Success);

            if (UserKey is null)
                Nav.NavigateTo("/login");
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to logout.", Severity.Error);
        }
    }

    private async Task RevokeOthersAsync()
    {
        var result = await UAuthClient.Sessions.RevokeMyOtherChainsAsync();
        if (result.IsSuccess)
        {
            Snackbar.Add("Revoked all other devices.", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to logout.", Severity.Error);
        }
    }

    private async Task RevokeChainAsync(SessionChainId chainId)
    {
        UAuthResult<RevokeResult> result;

        if (UserKey is null)
        {
            result = await UAuthClient.Sessions.RevokeMyChainAsync(chainId);
        }
        else
        {
            result = await UAuthClient.Sessions.RevokeUserChainAsync(UserKey.Value, chainId);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Device revoked successfully.", Severity.Success);

            if (result?.Value?.CurrentChain == true)
            {
                Nav.NavigateTo("/login");
                return;
            }
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to logout.", Severity.Error);
        }
    }

    private async Task ShowChainDetailsAsync(SessionChainId chainId)
    {
        UAuthResult<SessionChainDetail> result;

        if (UserKey is null)
        {
            result = await UAuthClient.Sessions.GetMyChainDetailAsync(chainId);
        }
        else
        {
            result = await UAuthClient.Sessions.GetUserChainDetailAsync(UserKey.Value, chainId);
        }

        if (result.IsSuccess)
        {
            _chainDetail = result.Value;
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to fetch chain details.", Severity.Error);
            _chainDetail = null;
        }
    }

    private void ClearDetail()
    {
        _chainDetail = null;
    }

    private void Cancel() => MudDialog.Cancel();
}
