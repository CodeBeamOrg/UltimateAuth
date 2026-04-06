using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Common;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Components.Dialogs;

public partial class UsersDialog
{
    private MudDataGrid<UserSummary>? _grid;
    private bool _loading;
    private string? _search;
    private bool _reloadQueued;
    private UserStatus? _statusFilter;

    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    private async Task<GridData<UserSummary>> LoadUsers(GridState<UserSummary> state, CancellationToken ct)
    {
        var sort = state.SortDefinitions?.FirstOrDefault();

        var req = new UserQuery
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            Search = _search,
            Status = _statusFilter,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        var res = await UAuthClient.Users.QueryAsync(req);

        if (!res.IsSuccess || res.Value == null)
        {
            Snackbar.Add(res.ErrorText ?? "Failed to load users.", Severity.Error);

            return new GridData<UserSummary>
            {
                Items = Array.Empty<UserSummary>(),
                TotalItems = 0
            };
        }

        return new GridData<UserSummary>
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

    private async Task OnStatusChanged(UserStatus? status)
    {
        _statusFilter = status;
        await ReloadAsync();
    }

    private async Task OpenUser(UserKey userKey)
    {
        var dialog = await DialogService.ShowAsync<UserDetailDialog>("User", UAuthDialog.GetDialogParameters(AuthState, userKey), UAuthDialog.GetDialogOptions());
        await dialog.Result;
        await ReloadAsync();
    }

    private async Task OpenCreateUser()
    {
        var dialog = await DialogService.ShowAsync<CreateUserDialog>(
            "Create User",
            new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = true
            });

        var result = await dialog.Result;

        if (result?.Canceled == false)
            await ReloadAsync();
    }

    private async Task DeleteUserAsync(UserSummary user)
    {
        var confirm = await DialogService.ShowMessageBoxAsync(
            title: "Delete user",
            markupMessage: (MarkupString)$"""
            Are you sure you want to delete <b>{user.DisplayName ?? user.UserName ?? user.PrimaryEmail ?? user.UserKey}</b>?
            <br/><br/>
            This operation is intended for admin usage.
            """,
            yesText: "Delete",
            cancelText: "Cancel",
            options: new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackgroundClass = "uauth-blur-slight"
            });

        if (confirm != true)
            return;

        var req = new DeleteUserRequest
        {
            Mode = DeleteMode.Soft
        };

        var result = await UAuthClient.Users.DeleteUserAsync(user.UserKey, req);

        if (result.IsSuccess)
        {
            Snackbar.Add("User deleted successfully.", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Failed to delete user.", Severity.Error);
        }
    }

    private static Color GetStatusColor(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => Color.Success,
            UserStatus.SelfSuspended => Color.Warning,
            UserStatus.Suspended => Color.Warning,
            UserStatus.Disabled => Color.Error,
            _ => Color.Default
        };
    }

    private void Close()
    {
        MudDialog.Close();
    }
}
