using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Sample.Shared.Common;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class UserDetailDialog
{
    private UserView? _user;
    private AdminAssignableUserStatus _status;

    private bool _isLoading = true;
    private bool _isSubmitting;
    private string? _loadError;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey UserKey { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private string DisplayName =>
        PreviewMode ? "Alex Morgan" : _user?.DisplayName ?? "—";

    private string UserName =>
        PreviewMode ? "alex.morgan" : _user?.UserName ?? "—";

    private string PrimaryEmail =>
        PreviewMode ? "alex@example.com" : _user?.PrimaryEmail ?? "—";

    private string PrimaryPhone =>
        PreviewMode ? "+1 555 010 1234" : _user?.PrimaryPhone ?? "—";

    private string CreatedAt =>
        PreviewMode
            ? "Oct 12, 2026"
            : _user?.CreatedAt?.ToLocalTime().ToString("g") ?? "—";

    private string UserKeyText =>
        PreviewMode ? "preview-user-001" : _user?.UserKey.Value ?? "—";

    private UserStatus? CurrentStatus =>
        PreviewMode ? UserStatus.Active : _user?.Status;

    private bool HasStatusChange =>
        _user is not null &&
        _status != _user.Status.ToAdminAssignableUserStatus();

    protected override async Task OnInitializedAsync()
    {
        if (PreviewMode)
        {
            _status = AdminAssignableUserStatus.Active;
            _isLoading = false;
            return;
        }

        await LoadUserAsync();
    }

    private async Task LoadUserAsync()
    {
        _isLoading = true;
        _loadError = null;

        try
        {
            var result = await UAuthClient.Users.GetUserAsync(UserKey);

            if (!result.IsSuccess || result.Value is null)
            {
                _loadError = result.ErrorText
                    ?? "Unable to load user details.";
                return;
            }

            _user = result.Value;
            _status = _user.Status.ToAdminAssignableUserStatus();
        }
        catch (Exception)
        {
            _loadError = "An unexpected error occurred while loading the user.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task ChangeStatusAsync()
    {
        if (PreviewMode || _isSubmitting || _user is null ||
            !HasStatusChange)
            return;

        _isSubmitting = true;

        try
        {
            var request = new ChangeUserStatusAdminRequest
            {
                NewStatus = _status
            };

            var result = await UAuthClient.Users
                .ChangeUserStatusAsync(_user.UserKey, request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to update user status.",
                    Severity.Error);
                return;
            }

            _user = _user with
            {
                Status = _status.ToUserStatus()
            };

            Snackbar.Add(
                "User status updated successfully.",
                Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to update user status.", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private DialogParameters CreateChildParameters()
    {
        var parameters = new DialogParameters
        {
            { "PreviewMode", PreviewMode }
        };

        if (!PreviewMode)
        {
            parameters.Add("AuthState", AuthState);
            parameters.Add("UserKey", _user!.UserKey);
        }

        return parameters;
    }

    private async Task OpenSessionsAsync()
    {
        await OpenChildAsync<SessionDialog>(
            "Session Management", MaxWidth.Large);
    }

    private async Task OpenProfileAsync()
    {
        await OpenChildAsync<ProfileDialog>(
            "Profile Management", MaxWidth.Medium);
    }

    private async Task OpenIdentifiersAsync()
    {
        await OpenChildAsync<IdentifierDialog>(
            "Identifier Management", MaxWidth.Medium);
    }

    private async Task OpenCredentialsAsync()
    {
        await OpenChildAsync<CredentialDialog>(
            "Credential Management", MaxWidth.Small);
    }

    private async Task OpenRolesAsync()
    {
        await OpenChildAsync<UserRoleDialog>(
            "User Roles", MaxWidth.Small);
    }

    private async Task OpenChildAsync<TDialog>(
        string title,
        MaxWidth maxWidth)
        where TDialog : IComponent
    {
        if (_isLoading || _isSubmitting)
            return;

        var dialog = await DialogService.ShowAsync<TDialog>(
            title,
            CreateChildParameters(),
            UAuthDialog.GetDialogOptions(maxWidth));

        await dialog.Result;

        if (!PreviewMode)
            await LoadUserAsync();
    }

    private static Color GetStatusColor(UserStatus? status)
    {
        return status switch
        {
            UserStatus.Active => Color.Success,
            UserStatus.Suspended => Color.Warning,
            UserStatus.Disabled => Color.Error,
            _ => Color.Default
        };
    }

    private void Close()
    {
        if (!_isSubmitting)
            MudDialog.Close();
    }
}
