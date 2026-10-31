using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Sample.Shared.Common;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class UsersDialog
{
    private MudDataGrid<UserSummary>? _grid;

    private bool _loading;
    private bool _reloadQueued;
    private string? _search;
    private UserStatus? _statusFilter;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private readonly List<PreviewUser> _previewUsers =
    [
        new(
            "preview-001",
            "Alex Morgan",
            "alex.morgan",
            "alex@example.com",
            UserStatus.Active,
            "Oct 12, 2026"),
        new(
            "preview-002",
            "Taylor Reed",
            "taylor.reed",
            "taylor@example.com",
            UserStatus.Active,
            "Oct 14, 2026"),
        new(
            "preview-003",
            "Jordan Lee",
            "jordan.lee",
            "jordan@example.com",
            UserStatus.Suspended,
            "Oct 15, 2026"),
        new(
            "preview-004",
            "Casey Parker",
            "casey.parker",
            "casey@example.com",
            UserStatus.Disabled,
            "Oct 16, 2026")
    ];

    private List<PreviewUser> FilteredPreviewUsers =>
        _previewUsers
            .Where(user =>
                (!_statusFilter.HasValue ||
                 user.Status == _statusFilter.Value) &&
                (string.IsNullOrWhiteSpace(_search) ||
                 user.DisplayName.Contains(
                     _search, StringComparison.OrdinalIgnoreCase) ||
                 user.UserName.Contains(
                     _search, StringComparison.OrdinalIgnoreCase) ||
                 user.Email.Contains(
                     _search, StringComparison.OrdinalIgnoreCase) ||
                 user.Key.Contains(
                     _search, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    private async Task<GridData<UserSummary>> LoadUsers(
        GridState<UserSummary> state,
        CancellationToken ct)
    {
        if (PreviewMode)
            return EmptyGrid();

        var sort = state.SortDefinitions?.FirstOrDefault();

        var request = new UserQuery
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            Search = _search,
            Status = _statusFilter,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        try
        {
            var result = await UAuthClient.Users.QueryAsync(request);

            if (!result.IsSuccess || result.Value is null)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to load users.",
                    Severity.Error);

                return EmptyGrid();
            }

            return new GridData<UserSummary>
            {
                Items = result.Value.Items,
                TotalItems = result.Value.TotalCount
            };
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to load users.", Severity.Error);
            return EmptyGrid();
        }
    }

    private static GridData<UserSummary> EmptyGrid() => new()
    {
        Items = Array.Empty<UserSummary>(),
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

    private async Task OnSearchChanged(string? value)
    {
        _search = value;

        if (!PreviewMode)
            await ReloadAsync();
    }

    private async Task OnStatusChanged(UserStatus? value)
    {
        _statusFilter = value;

        if (!PreviewMode)
            await ReloadAsync();
    }

    private async Task OpenPreviewUserAsync()
    {
        var parameters = new DialogParameters<UserDetailDialog>
        {
            { x => x.PreviewMode, true }
        };

        await DialogService.ShowAsync<UserDetailDialog>(
            "User Management",
            parameters,
            UAuthDialog.GetDialogOptions(MaxWidth.Medium));
    }

    private async Task OpenUserAsync(UserKey userKey)
    {
        if (PreviewMode || _loading)
            return;

        var parameters = new DialogParameters<UserDetailDialog>
        {
            { x => x.UserKey, userKey },
            { x => x.AuthState, AuthState }
        };

        var dialog = await DialogService.ShowAsync<UserDetailDialog>(
            "User Management",
            parameters,
            UAuthDialog.GetDialogOptions(MaxWidth.Medium));

        await dialog.Result;
        await ReloadAsync();
    }

    private async Task OpenCreateUserAsync()
    {
        if (_loading)
            return;

        var parameters = new DialogParameters<CreateUserDialog>
        {
            { x => x.PreviewMode, PreviewMode }
        };

        var dialog = await DialogService.ShowAsync<CreateUserDialog>(
            "Create User",
            parameters,
            UAuthDialog.GetDialogOptions(MaxWidth.Small));

        var result = await dialog.Result;

        if (!PreviewMode && result?.Canceled == false)
            await ReloadAsync();
    }

    private async Task DeleteUserAsync(UserSummary user)
    {
        if (PreviewMode || _loading)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete User",
            $"Delete '{user.DisplayName ?? user.UserName ?? user.PrimaryEmail}'? " +
            "This operation uses soft deletion.",
            yesText: "Delete",
            noText: "Cancel");

        if (confirmed != true || _loading)
            return;

        _loading = true;
        bool success = false;

        try
        {
            var request = new DeleteUserRequest
            {
                Mode = DeleteMode.Soft
            };

            var result = await UAuthClient.Users
                .DeleteUserAsync(user.UserKey, request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to delete user.",
                    Severity.Error);
                return;
            }

            success = true;
            Snackbar.Add(
                "User deleted successfully.",
                Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to delete user.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }

        if (success)
            await ReloadAsync();
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
        if (!_loading)
            MudDialog.Close();
    }

    private sealed record PreviewUser(
        string Key,
        string DisplayName,
        string UserName,
        string Email,
        UserStatus Status,
        string CreatedAt);
}
