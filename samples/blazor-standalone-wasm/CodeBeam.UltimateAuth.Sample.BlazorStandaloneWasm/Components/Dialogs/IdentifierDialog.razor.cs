using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class IdentifierDialog
{
    private MudDataGrid<UserIdentifierInfo>? _grid;
    private UserIdentifierType _newIdentifierType;
    private string? _newIdentifierValue;
    private bool _newIdentifierPrimary;
    private bool _loading = false;
    private bool _reloadQueued;
    private bool _loaded;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    [Parameter]
    public UserKey? UserKey { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        
        if (firstRender)
        {
            _loaded = true;
            var result = await UAuthClient.Identifiers.GetMyAsync();
            if (result != null && result.IsSuccess && result.Value != null)
            {
                await ReloadAsync();
            }
            StateHasChanged();
        }
    }

    private async Task<GridData<UserIdentifierInfo>> LoadServerData(GridState<UserIdentifierInfo> state, CancellationToken ct)
    {
        var sort = state.SortDefinitions?.FirstOrDefault();

        var req = new PageRequest
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            SortBy = sort?.SortBy,
            Descending = sort?.Descending ?? false
        };

        UAuthResult<PagedResult<UserIdentifierInfo>> res;

        if (UserKey is null)
        {
            res = await UAuthClient.Identifiers.GetMyAsync(req);
        }
        else
        {
            res = await UAuthClient.Identifiers.GetUserAsync(UserKey.Value, req);
        }

        if (!res.IsSuccess || res.Value is null)
        {
            Snackbar.Add(res.Problem?.Title ?? "Failed", Severity.Error);

            return new GridData<UserIdentifierInfo>
            {
                Items = Array.Empty<UserIdentifierInfo>(),
                TotalItems = 0
            };
        }

        return new GridData<UserIdentifierInfo>
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
        await Task.Delay(300);

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

    private async Task<DataGridEditFormAction> CommittedItemChanges(UserIdentifierInfo item)
    {
        UpdateUserIdentifierRequest updateRequest = new()
        {
            Id = item.Id,
            NewValue = item.Value
        };

        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Identifiers.UpdateMyAsync(updateRequest);
        }
        else
        {
            result = await UAuthClient.Identifiers.UpdateUserAsync(UserKey.Value, updateRequest);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Identifier updated successfully", Severity.Success);
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to update identifier", Severity.Error);
        }

        await ReloadAsync();
        return DataGridEditFormAction.Close;
    }

    private async Task AddNewIdentifier()
    {
        if (string.IsNullOrEmpty(_newIdentifierValue))
        {
            Snackbar.Add("Value cannot be empty", Severity.Warning);
            return;
        }

        AddUserIdentifierRequest request = new()
        {
            Type = _newIdentifierType,
            Value = _newIdentifierValue,
            IsPrimary = _newIdentifierPrimary
        };

        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Identifiers.AddMyAsync(request);
        }
        else
        {
            result = await UAuthClient.Identifiers.AddUserAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Identifier added successfully", Severity.Success);
            await ReloadAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to add identifier", Severity.Error);
        }
    }

    private async Task VerifyAsync(Guid id)
    {
        var demoInfo = await DialogService.ShowMessageBoxAsync(
            title: "Demo verification",
            markupMessage: (MarkupString)
                """
            This is a <b>demo</b> action.<br/><br/>
            In a real app, you should verify identifiers via <b>Email</b>, <b>SMS</b>, or an <b>Authenticator</b> flow.
            This will only mark the identifier as verified in UltimateAuth.
            """,
            yesText: "Verify", noText: "Cancel",
            options: new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "uauth-blur-slight" });

        if (demoInfo != true)
        {
            Snackbar.Add("Verification cancelled", Severity.Info);
            return;
        }

        VerifyUserIdentifierRequest request = new() { Id = id };
        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Identifiers.VerifyMyAsync(request);
        }
        else
        {
            result = await UAuthClient.Identifiers.VerifyUserAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Identifier verified successfully", Severity.Success);
            await ReloadAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to verify primary identifier", Severity.Error);
        }
    }

    private async Task SetPrimaryAsync(Guid id)
    {
        SetPrimaryUserIdentifierRequest request = new() { Id = id };
        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Identifiers.SetMyPrimaryAsync(request);
        }
        else
        {
            result = await UAuthClient.Identifiers.SetUserPrimaryAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Primary identifier set successfully.", Severity.Success);
            await ReloadAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to set primary identifier", Severity.Error);
        }
    }

    private async Task UnsetPrimaryAsync(Guid id)
    {
        UnsetPrimaryUserIdentifierRequest request = new() { Id = id };
        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Identifiers.UnsetMyPrimaryAsync(request);
        }
        else
        {
            result = await UAuthClient.Identifiers.UnsetUserPrimaryAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Primary identifier unset successfully.", Severity.Success);
            await ReloadAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to unset primary identifier", Severity.Error);
        }
    }

    private async Task DeleteIdentifier(Guid id)
    {
        DeleteUserIdentifierRequest request = new() { Id = id };
        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Identifiers.DeleteMyAsync(request);
        }
        else
        {
            result = await UAuthClient.Identifiers.DeleteUserAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Identifier deleted successfully.", Severity.Success);
            await ReloadAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to delete identifier", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
