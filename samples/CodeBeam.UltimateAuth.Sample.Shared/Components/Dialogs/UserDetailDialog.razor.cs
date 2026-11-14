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

    private bool _securityLoading;
    private bool _securityBusy;
    private string? _securityError;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey UserKey { get; set; }

    [Parameter]
    public bool AllowRoleManagement { get; set; } = true;

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

        if (_user is not null)
            await LoadSecurityAsync();
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


    private sealed class SecurityItem(
        string title,
        AuthenticationSecurityScope scope,
        CredentialType? credentialType = null)
    {
        public string Title { get; } = title;
        public AuthenticationSecurityScope Scope { get; } = scope;
        public CredentialType? CredentialType { get; } = credentialType;

        public AuthenticationLockoutInfo? State { get; set; }
        public bool Unlocking { get; set; }
    }

    private readonly List<SecurityItem> _securityItems =
    [
        new(
        "Account Authentication",
        AuthenticationSecurityScope.Account),

    new(
        "Password Authentication",
        AuthenticationSecurityScope.Factor,
        CredentialType.Password)
    ];

    private static string FormatSecurityTime(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("dd MMM yyyy HH:mm:ss") ?? "—";

    private static bool CanUnlock(SecurityItem item)
    {
        var state = item.State;

        return state is not null && (state.IsLocked || state.FailedAttempts > 0 || state.LastFailedAt is not null);
    }

    private async Task LoadSecurityAsync()
    {
        if (PreviewMode || _securityLoading || _securityBusy)
            return;

        _securityLoading = true;
        _securityError = null;

        try
        {
            foreach (var item in _securityItems)
            {
                // Do not display stale security information.
                item.State = null;

                var request = new LockoutQueryRequest(item.Scope, item.CredentialType);

                var result = await UAuthClient.Users.GetAuthenticationLockoutAsync(UserKey, request);

                if (!result.IsSuccess || result.Value is null)
                {
                    _securityError = result.ErrorText ?? $"Unable to retrieve {item.Title} state.";
                    continue;
                }

                item.State = result.Value;
            }
        }
        catch (Exception)
        {
            _securityError = "Unable to retrieve authentication security state.";
        }
        finally
        {
            _securityLoading = false;
        }
    }

    private async Task UnlockSecurityAsync(SecurityItem item)
    {
        if (PreviewMode || _securityLoading ||
            _securityBusy || !CanUnlock(item))
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Unlock Authentication",
            $"Clear authentication failures and lockout for " +
            $"'{DisplayName}' ({item.Title})?",
            yesText: "Unlock",
            noText: "Cancel");

        if (confirmed != true || _securityBusy)
            return;

        _securityBusy = true;
        item.Unlocking = true;
        _securityError = null;

        try
        {
            var request = new UnlockAuthenticationRequest(
                item.Scope,
                item.CredentialType);

            var result = await UAuthClient.Users
                .UnlockAuthenticationAsync(
                    UserKey,
                    request);

            if (!result.IsSuccess || result.Value is null)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Unable to unlock authentication.",
                    Severity.Error);
                return;
            }

            // The response contains the resulting security state.
            item.State = result.Value.State;

            Snackbar.Add(
                $"{item.Title} unlocked successfully.",
                Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add(
                "Unable to unlock authentication.",
                Severity.Error);
        }
        finally
        {
            item.Unlocking = false;
            _securityBusy = false;
        }

        // Read back from the server to verify the final state.
        await LoadSecurityAsync();
    }

    private void Close()
    {
        if (!_isSubmitting)
            MudDialog.Close();
    }
}
