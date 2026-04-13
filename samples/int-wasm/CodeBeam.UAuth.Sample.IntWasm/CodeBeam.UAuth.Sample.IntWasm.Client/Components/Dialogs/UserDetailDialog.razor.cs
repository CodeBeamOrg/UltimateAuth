using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UAuth.Sample.IntWasm.Client.Common;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UAuth.Sample.IntWasm.Client.Components.Dialogs;

public partial class UserDetailDialog
{
    private UserView? _user;
    private AdminAssignableUserStatus _status;

    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    [Parameter]
    public UserKey UserKey { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var result = await UAuthClient.Users.GetUserAsync(UserKey);

        if (result.IsSuccess)
        {
            _user = result.Value;
            _status = _user?.Status.ToAdminAssignableUserStatus() ?? AdminAssignableUserStatus.Unknown;
        }
    }

    private async Task OpenSessions()
    {
        await DialogService.ShowAsync<SessionDialog>("Session Management", UAuthDialog.GetDialogParameters(AuthState, _user?.UserKey), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenProfile()
    {
        await DialogService.ShowAsync<ProfileDialog>("Profile Management", UAuthDialog.GetDialogParameters(AuthState, _user?.UserKey), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenIdentifiers()
    {
        await DialogService.ShowAsync<IdentifierDialog>("Identifier Management", UAuthDialog.GetDialogParameters(AuthState, _user?.UserKey), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenCredentials()
    {
        await DialogService.ShowAsync<CredentialDialog>("Credentials", UAuthDialog.GetDialogParameters(AuthState, _user?.UserKey), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenRoles()
    {
        await DialogService.ShowAsync<UserRoleDialog>("Roles", UAuthDialog.GetDialogParameters(AuthState, _user?.UserKey), UAuthDialog.GetDialogOptions());
    }

    private async Task ChangeStatusAsync()
    {
        if (_user is null)
            return;

        ChangeUserStatusAdminRequest request = new()
        {
            NewStatus = _status,
        };

        var result = await UAuthClient.Users.ChangeUserStatusAsync(_user.UserKey, request);

        if (result.IsSuccess)
        {
            Snackbar.Add("User status updated", Severity.Success);
            _user = _user with { Status = _status.ToUserStatus() };
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Failed", Severity.Error);
        }
    }

    private Color GetStatusColor(UserStatus? status)
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
        MudDialog.Close();
    }
}
