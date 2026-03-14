using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class RoleDialog
{
    private MudDataGrid<RoleInfo>? _grid;
    private bool _loading;
    private string? _newRoleName;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    private async Task<GridData<RoleInfo>> LoadServerData(GridState<RoleInfo> state, CancellationToken ct)
    {
        var sort = state.SortDefinitions?.FirstOrDefault();

        var req = new RoleQuery
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        var res = await UAuthClient.Authorization.QueryRolesAsync(req);

        if (!res.IsSuccess || res.Value == null)
        {
            Snackbar.Add(res.GetErrorText ?? "Failed", Severity.Error);

            return new GridData<RoleInfo>
            {
                Items = Array.Empty<RoleInfo>(),
                TotalItems = 0
            };
        }

        return new GridData<RoleInfo>
        {
            Items = res.Value.Items,
            TotalItems = res.Value.TotalCount
        };
    }

    private async Task<DataGridEditFormAction> CommittedItemChanges(RoleInfo role)
    {
        var req = new RenameRoleRequest
        {
            Name = role.Name
        };

        var result = await UAuthClient.Authorization.RenameRoleAsync(role.Id, req);

        if (result.IsSuccess)
        {
            Snackbar.Add("Role renamed", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.GetErrorText ?? "Rename failed", Severity.Error);
        }

        await ReloadAsync();
        return DataGridEditFormAction.Close;
    }

    private async Task CreateRole()
    {
        if (string.IsNullOrWhiteSpace(_newRoleName))
        {
            Snackbar.Add("Role name required.", Severity.Warning);
            return;
        }

        var req = new CreateRoleRequest
        {
            Name = _newRoleName
        };

        var res = await UAuthClient.Authorization.CreateRoleAsync(req);

        if (res.IsSuccess)
        {
            Snackbar.Add("Role created.", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add(res.GetErrorText ?? "Creation failed.", Severity.Error);
        }
    }

    private async Task DeleteRole(RoleId roleId)
    {
        var confirm = await DialogService.ShowMessageBoxAsync(
            "Delete role",
            "Are you sure?",
            yesText: "Delete",
            cancelText: "Cancel",
            options: new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "uauth-blur-slight" });

        if (confirm != true)
            return;

        var req = new DeleteRoleRequest();
        var result = await UAuthClient.Authorization.DeleteRoleAsync(roleId, req);

        if (result.IsSuccess)
        {
            Snackbar.Add($"Role deleted, assignments removed from {result.Value?.RemovedAssignments.ToString() ?? "unknown"} users.", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add(result.GetErrorText ?? "Deletion failed.", Severity.Error);
        }
    }

    private async Task EditPermissions(RoleInfo role)
    {
        var dialog = await DialogService.ShowAsync<PermissionDialog>(
            "Edit Permissions",
            new DialogParameters
            {
            { nameof(PermissionDialog.Role), role }
            },
            new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            });

        var result = await dialog.Result;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        await Task.Delay(300);
        if (_grid is null)
            return;

        await _grid.ReloadServerData();
        _loading = false;
    }

    private int GetPermissionCount(RoleInfo role)
    {
        var expanded = PermissionExpander.Expand(role.Permissions, UAuthPermissionCatalog.GetAdminPermissions());
        return expanded.Count;
    }

    private void Cancel() => MudDialog.Cancel();
}
