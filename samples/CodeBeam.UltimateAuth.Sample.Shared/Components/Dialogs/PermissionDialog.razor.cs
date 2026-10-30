using CodeBeam.UltimateAuth.Authorization.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class PermissionDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public RoleInfo? Role { get; set; }

    [Parameter]
    public string PreviewRoleName { get; set; } = "Editor";

    [Parameter]
    public IReadOnlyList<string>? AvailablePermissions { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private List<PermissionGroup> _groups = [];
    private HashSet<string> _originalSelection = [];

    private string? _search;
    private bool _isSaving;

    private int SelectedCount =>
        _groups.Sum(g => g.Items.Count(x => x.Selected));

    private int TotalCount =>
        _groups.Sum(g => g.Items.Count);

    private double SelectionPercentage =>
        TotalCount == 0
            ? 0
            : SelectedCount * 100.0 / TotalCount;

    private bool HasChanges =>
        !_originalSelection.SetEquals(
            _groups.SelectMany(g => g.Items)
                   .Where(x => x.Selected)
                   .Select(x => x.Value));

    private List<PermissionGroup> FilteredGroups =>
        _groups
            .Where(g =>
                string.IsNullOrWhiteSpace(_search) ||
                g.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                g.Items.Any(x => x.Value.Contains(
                    _search, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    protected override void OnInitialized()
    {
        var catalog = AvailablePermissions ?? UAuthPermissionCatalog.GetAdminPermissions();

        HashSet<string> selected;

        if (PreviewMode)
        {
            selected = catalog
                .Take(Math.Min(6, catalog.Count()))
                .ToHashSet(StringComparer.Ordinal);
        }
        else
        {
            if (Role is null)
                throw new InvalidOperationException(
                    "Role is required outside preview mode.");

            var expanded = PermissionExpander.Expand(
                Role.Permissions,
                catalog);

            selected = expanded
                .Select(x => x.Value)
                .ToHashSet(StringComparer.Ordinal);
        }

        _originalSelection = new HashSet<string>(
            selected,
            StringComparer.Ordinal);

        _groups = catalog
            .GroupBy(p => p.Split('.')[0])
            .Select(group => new PermissionGroup
            {
                Name = group.Key,
                Expanded = false,
                Items = group
                    .Select(permission => new PermissionItem
                    {
                        Value = permission,
                        Selected = selected.Contains(permission)
                    })
                    .ToList()
            })
            .OrderBy(x => x.Name)
            .ToList();
    }

    private IEnumerable<PermissionItem> GetVisibleItems(
        PermissionGroup group)
    {
        if (string.IsNullOrWhiteSpace(_search))
            return group.Items;

        return group.Items.Where(x =>
            x.Value.Contains(_search, StringComparison.OrdinalIgnoreCase));
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

    private void ToggleGroup(PermissionGroup group, bool selected)
    {
        if (_isSaving)
            return;

        // A group checkbox always affects the complete group,
        // even when search filtering is active.
        foreach (var item in group.Items)
            item.Selected = selected;
    }

    private void TogglePermission(
        PermissionItem permission,
        bool selected)
    {
        if (_isSaving)
            return;

        permission.Selected = selected;
    }

    private void ToggleExpanded(PermissionGroup group)
    {
        group.Expanded = !group.Expanded;
    }

    private void ResetSelection()
    {
        if (_isSaving)
            return;

        foreach (var item in _groups.SelectMany(g => g.Items))
        {
            item.Selected = _originalSelection.Contains(item.Value);
        }
    }

    private async Task SaveAsync()
    {
        if (PreviewMode || _isSaving || Role is null || !HasChanges)
            return;

        var permissions = _groups
            .SelectMany(g => g.Items)
            .Where(x => x.Selected)
            .Select(x => Permission.From(x.Value))
            .ToList();

        var request = new SetRolePermissionsRequest
        {
            RoleId = Role.Id,
            Permissions = permissions
        };

        _isSaving = true;

        try
        {
            var result = await UAuthClient.Authorization
                .SetRolePermissionsAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to update permissions.",
                    Severity.Error);
                return;
            }

            Snackbar.Add(
                "Role permissions updated successfully.",
                Severity.Success);

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred while updating permissions.",
                Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void Cancel()
    {
        if (!_isSaving)
            MudDialog.Cancel();
    }

    private sealed class PermissionGroup
    {
        public string Name { get; set; } = "";
        public bool Expanded { get; set; }
        public List<PermissionItem> Items { get; set; } = [];
    }

    private sealed class PermissionItem
    {
        public string Value { get; set; } = "";
        public bool Selected { get; set; }
    }
}
