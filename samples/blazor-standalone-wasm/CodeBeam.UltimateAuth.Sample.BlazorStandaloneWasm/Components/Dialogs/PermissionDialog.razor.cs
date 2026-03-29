using CodeBeam.UltimateAuth.Authorization.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class PermissionDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public RoleInfo Role { get; set; } = default!;

    private List<PermissionGroup> _groups = new();

    protected override void OnInitialized()
    {
        var catalog = UAuthPermissionCatalog.GetAdminPermissions();
        var expanded = PermissionExpander.Expand(Role.Permissions, catalog);
        var selected = expanded.Select(x => x.Value).ToHashSet();

        _groups = catalog
            .GroupBy(p => p.Split('.')[0])
            .Select(g => new PermissionGroup
            {
                Name = g.Key,
                Items = g.Select(p => new PermissionItem
                {
                    Value = p,
                    Selected = selected.Contains(p)
                }).ToList()
            })
            .OrderBy(x => x.Name)
            .ToList();
    }

    private void ToggleGroup(PermissionGroup group, bool value)
    {
        foreach (var item in group.Items)
            item.Selected = value;
    }

    private void TogglePermission(PermissionItem item, bool value)
    {
        item.Selected = value;
    }

    private bool? GetGroupState(PermissionGroup group)
    {
        var selected = group.Items.Count(x => x.Selected);

        if (selected == 0)
            return false;

        if (selected == group.Items.Count)
            return true;

        return null;
    }

    private async Task Save()
    {
        var permissions = _groups.SelectMany(g => g.Items).Where(x => x.Selected).Select(x => Permission.From(x.Value)).ToList();

        var req = new SetRolePermissionsRequest
        {
            RoleId = Role.Id,
            Permissions = permissions
        };

        var result = await UAuthClient.Authorization.SetRolePermissionsAsync(req);

        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorText ?? "Failed to update permissions", Severity.Error);
            return;
        }

        var result2 = await UAuthClient.Authorization.QueryRolesAsync(new RoleQuery() { Search = Role.Name });
        if (result2.Value?.Items is not null)
        {
            Role = result2.Value.Items.First();
        }

        Snackbar.Add("Permissions updated", Severity.Success);
        RefreshUI();
    }

    private void RefreshUI()
    {
        var catalog = UAuthPermissionCatalog.GetAdminPermissions();
        var expanded = PermissionExpander.Expand(Role.Permissions, catalog);
        var selected = expanded.Select(x => x.Value).ToHashSet();

        foreach (var group in _groups)
        {
            foreach (var item in group.Items)
            {
                item.Selected = selected.Contains(item.Value);
            }
        }

        StateHasChanged();
    }

    private void Cancel() => MudDialog.Cancel();

    private class PermissionGroup
    {
        public string Name { get; set; } = "";
        public List<PermissionItem> Items { get; set; } = new();
    }

    private class PermissionItem
    {
        public string Value { get; set; } = "";
        public bool Selected { get; set; }
    }
}
