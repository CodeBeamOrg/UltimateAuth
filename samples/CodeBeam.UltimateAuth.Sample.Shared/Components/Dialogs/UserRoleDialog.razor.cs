using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class UserRoleDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey UserKey { get; set; } = default!;

    [Parameter]
    public bool PreviewMode { get; set; }

    private List<string> _roles = [];
    private List<RoleInfo> _allRoles = [];

    private string? _selectedRole;
    private bool _isLoading = true;
    private bool _isSubmitting;
    private string? _loadError;

    private List<RoleInfo> AvailableRoles =>
        _allRoles
            .Where(role => !_roles.Contains(
                role.Name,
                StringComparer.OrdinalIgnoreCase))
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        if (PreviewMode)
        {
            LoadPreviewData();
            _isLoading = false;
            return;
        }

        await LoadRolesAsync();
    }

    private void LoadPreviewData()
    {
        _roles = ["User", "Editor"];
        _allRoles =
        [
            new RoleInfo { Name = "User" },
            new RoleInfo { Name = "Editor" },
            new RoleInfo { Name = "Admin" },
            new RoleInfo { Name = "Viewer" }
        ];
    }

    private async Task LoadRolesAsync()
    {
        _isLoading = true;
        _loadError = null;

        try
        {
            var userRoles = await UAuthClient.Authorization
                .GetUserRolesAsync(UserKey);

            if (!userRoles.IsSuccess || userRoles.Value is null)
            {
                _loadError = userRoles.ErrorText
                    ?? "Unable to load assigned roles.";
                return;
            }

            var roles = await UAuthClient.Authorization
                .QueryRolesAsync(new RoleQuery
                {
                    PageNumber = 1,
                    PageSize = 200
                });

            if (!roles.IsSuccess || roles.Value is null)
            {
                _loadError = roles.ErrorText
                    ?? "Unable to load available roles.";
                return;
            }

            _roles = userRoles.Value.Roles.Items
                .Select(x => x.Name)
                .ToList();

            _allRoles = roles.Value.Items.ToList();
        }
        catch (Exception)
        {
            _loadError = "An unexpected error occurred while loading roles.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task AddRoleAsync()
    {
        if (PreviewMode || _isSubmitting ||
            string.IsNullOrWhiteSpace(_selectedRole))
            return;

        var roleName = _selectedRole;

        if (_roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            return;

        var request = new AssignRoleRequest
        {
            UserKey = UserKey,
            RoleName = roleName
        };

        _isSubmitting = true;

        try
        {
            var result = await UAuthClient.Authorization
                .AssignRoleToUserAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to assign role.",
                    Severity.Error);
                return;
            }

            _roles.Add(roleName);
            _selectedRole = null;

            Snackbar.Add("Role assigned successfully.", Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("An unexpected error occurred.", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task RemoveRoleAsync(string role)
    {
        if (PreviewMode || _isSubmitting)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Remove Role",
            $"Remove the '{role}' role from this user?",
            yesText: "Remove",
            noText: "Cancel");

        if (confirmed != true)
            return;

        if (IsAdminRole(role))
        {
            var adminConfirmed = await DialogService.ShowMessageBoxAsync(
                "Remove Administrator Role",
                "Removing the Admin role may prevent this user from " +
                "managing the application. Continue?",
                yesText: "Remove Admin",
                noText: "Cancel");

            if (adminConfirmed != true)
                return;
        }

        _isSubmitting = true;

        try
        {
            var request = new RemoveRoleRequest
            {
                UserKey = UserKey,
                RoleName = role
            };

            var result = await UAuthClient.Authorization
                .RemoveRoleFromUserAsync(request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to remove role.",
                    Severity.Error);
                return;
            }

            _roles.RemoveAll(x =>
                string.Equals(x, role, StringComparison.OrdinalIgnoreCase));

            Snackbar.Add("Role removed successfully.", Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("An unexpected error occurred.", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private static bool IsAdminRole(string role) =>
        string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

    private void Close()
    {
        if (!_isSubmitting)
            MudDialog.Close();
    }
}
