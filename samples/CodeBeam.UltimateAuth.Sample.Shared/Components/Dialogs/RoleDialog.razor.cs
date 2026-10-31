using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class RoleDialog
{
    private MudDataGrid<RoleInfo>? _grid;

    private bool _loading;
    private string? _newRoleName;
    private RoleInfo? _editingRole;
    private string? _editingName;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private readonly List<PreviewRole> _previewRoles =
    [
        new("Admin", "Full administrative access.", 24),
        new("Editor", "Manage application content.", 8),
        new("User", "Standard user permissions.", 3),
        new("Viewer", "Read-only access.", 2)
    ];

    private async Task<GridData<RoleInfo>> LoadServerData(
        GridState<RoleInfo> state,
        CancellationToken ct)
    {
        if (PreviewMode)
            return EmptyGrid();

        var sort = state.SortDefinitions?.FirstOrDefault();

        var request = new RoleQuery
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        try
        {
            var result = await UAuthClient.Authorization
                .QueryRolesAsync(request);

            if (!result.IsSuccess || result.Value is null)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to load roles.",
                    Severity.Error);

                return EmptyGrid();
            }

            return new GridData<RoleInfo>
            {
                Items = result.Value.Items,
                TotalItems = result.Value.TotalCount
            };
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to load roles.", Severity.Error);
            return EmptyGrid();
        }
    }

    private static GridData<RoleInfo> EmptyGrid() => new()
    {
        Items = Array.Empty<RoleInfo>(),
        TotalItems = 0
    };

    private async Task ReloadAsync()
    {
        if (PreviewMode || _grid is null || _loading)
            return;

        await _grid.ReloadServerData();
    }

    private void BeginRename(RoleInfo role)
    {
        if (PreviewMode || _loading)
            return;

        _editingRole = role;
        _editingName = role.Name;
    }

    private void CancelRename()
    {
        _editingRole = null;
        _editingName = null;
    }

    private async Task SaveRenameAsync()
    {
        if (PreviewMode || _loading || _editingRole is null ||
            string.IsNullOrWhiteSpace(_editingName))
            return;

        var request = new RenameRoleRequest
        {
            Id = _editingRole.Id,
            Name = _editingName.Trim()
        };

        _loading = true;
        bool success = false;

        try
        {
            var result = await UAuthClient.Authorization
                .RenameRoleAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to rename role.",
                    Severity.Error);
                return;
            }

            success = true;
            Snackbar.Add("Role renamed successfully.", Severity.Success);
            CancelRename();
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to rename role.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }

        if (success)
            await ReloadAsync();
    }

    private async Task CreateRoleAsync()
    {
        if (PreviewMode || _loading ||
            string.IsNullOrWhiteSpace(_newRoleName))
            return;

        var request = new CreateRoleRequest
        {
            Name = _newRoleName.Trim()
        };

        _loading = true;
        bool success = false;

        try
        {
            var result = await UAuthClient.Authorization
                .CreateRoleAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to create role.",
                    Severity.Error);
                return;
            }

            success = true;
            _newRoleName = null;
            Snackbar.Add("Role created successfully.", Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to create role.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }

        if (success)
            await ReloadAsync();
    }

    private async Task DeleteRoleAsync(RoleInfo role)
    {
        if (PreviewMode || _loading)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Role",
            $"Delete the '{role.Name}' role? Existing assignments " +
            "to users may also be removed.",
            yesText: "Delete",
            noText: "Cancel");

        if (confirmed != true || _loading)
            return;

        _loading = true;
        bool success = false;

        try
        {
            var request = new DeleteRoleRequest
            {
                Id = role.Id
            };

            var result = await UAuthClient.Authorization
                .DeleteRoleAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to delete role.",
                    Severity.Error);
                return;
            }

            success = true;

            Snackbar.Add(
                $"Role deleted. Assignments removed from " +
                $"{result.Value?.RemovedAssignments.ToString() ?? "unknown"} users.",
                Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to delete role.", Severity.Error);
        }
        finally
        {
            _loading = false;
        }

        if (success)
            await ReloadAsync();
    }

    private async Task EditPermissions(RoleInfo role)
    {
        if (PreviewMode || _loading)
            return;

        var parameters = new DialogParameters
        {
            { nameof(PermissionDialog.Role), role }
        };

        var dialog = await DialogService.ShowAsync<PermissionDialog>(
            "Edit Permissions",
            parameters,
            new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            });

        await dialog.Result;
        await ReloadAsync();
    }

    private static int GetPermissionCount(RoleInfo role)
    {
        var expanded = PermissionExpander.Expand(
            role.Permissions,
            UAuthPermissionCatalog.GetAdminPermissions());

        return expanded.Count;
    }

    private void Close()
    {
        if (!_loading)
            MudDialog.Close();
    }

    private sealed record PreviewRole(
        string Name,
        string Description,
        int PermissionCount);
}
