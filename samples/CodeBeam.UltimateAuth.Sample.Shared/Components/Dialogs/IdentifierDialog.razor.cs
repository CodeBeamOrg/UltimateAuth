using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class IdentifierDialog
{
    private readonly List<UserIdentifierInfo> _identifiers = [];
    private readonly UserIdentifierType[] _identifierTypes =
        Enum.GetValues<UserIdentifierType>();

    private UserIdentifierType _newIdentifierType = UserIdentifierType.Email;
    private string? _newIdentifierValue;
    private bool _newIdentifierPrimary;

    private Guid? _editingId;
    private string? _editingValue;

    private bool _loading;
    private string? _loadError;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey? UserKey { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    [Parameter]
    public bool EnableVerification { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        if (PreviewMode)
        {
            LoadPreviewData();
            return;
        }

        await ReloadAsync();
    }

    private void LoadPreviewData()
    {
        _identifiers.Clear();

        // Preview data is presentation-only.
        // Real identifier models are loaded from UltimateAuth.
        _identifiers.Add(new UserIdentifierInfo
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Type = UserIdentifierType.Username,
            Value = "alex",
            IsPrimary = true,
            IsVerified = true
        });

        _identifiers.Add(new UserIdentifierInfo
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Type = UserIdentifierType.Email,
            Value = "alex@example.com",
            IsPrimary = false,
            IsVerified = true
        });
    }

    private async Task ReloadAsync()
    {
        if (_loading || PreviewMode)
            return;

        _loading = true;
        _loadError = null;

        try
        {
            var collected = new List<UserIdentifierInfo>();
            const int pageSize = 50;
            var pageNumber = 1;

            while (true)
            {
                var request = new PageRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                UAuthResult<PagedResult<UserIdentifierInfo>> result =
                    UserKey is null
                        ? await UAuthClient.Identifiers.GetMyAsync(request)
                        : await UAuthClient.Identifiers.GetUserAsync(
                            UserKey.Value, request);

                if (!result.IsSuccess || result.Value is null)
                {
                    _loadError = result.ErrorText
                        ?? "Unable to load identifiers.";
                    return;
                }

                collected.AddRange(result.Value.Items);

                if (collected.Count >= result.Value.TotalCount ||
                    result.Value.Items.Count() == 0)
                    break;

                pageNumber++;
            }

            _identifiers.Clear();
            _identifiers.AddRange(collected);
        }
        catch (Exception)
        {
            _loadError = "An unexpected error occurred while loading identifiers.";
        }
        finally
        {
            _loading = false;
        }
    }

    private void BeginEdit(UserIdentifierInfo identifier)
    {
        if (PreviewMode || _loading)
            return;

        _editingId = identifier.Id;
        _editingValue = identifier.Value;
    }

    private void CancelEdit()
    {
        _editingId = null;
        _editingValue = null;
    }

    private async Task SaveEditAsync()
    {
        if (_editingId is null || string.IsNullOrWhiteSpace(_editingValue))
            return;

        var request = new UpdateUserIdentifierRequest
        {
            Id = _editingId.Value,
            NewValue = _editingValue.Trim()
        };

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Identifiers.UpdateMyAsync(request)
                : UAuthClient.Identifiers.UpdateUserAsync(
                    UserKey.Value, request),
            "Identifier updated successfully.");

        CancelEdit();
    }

    private async Task AddNewIdentifierAsync()
    {
        if (string.IsNullOrWhiteSpace(_newIdentifierValue))
        {
            Snackbar.Add("Identifier value is required.", Severity.Warning);
            return;
        }

        var request = new AddUserIdentifierRequest
        {
            Type = _newIdentifierType,
            Value = _newIdentifierValue.Trim(),
            IsPrimary = _newIdentifierPrimary
        };

        var success = await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Identifiers.AddMyAsync(request)
                : UAuthClient.Identifiers.AddUserAsync(
                    UserKey.Value, request),
            "Identifier added successfully.");

        if (success)
        {
            _newIdentifierValue = null;
            _newIdentifierPrimary = false;
        }
    }

    private async Task SetPrimaryAsync(Guid id)
    {
        var request = new SetPrimaryUserIdentifierRequest { Id = id };

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Identifiers.SetMyPrimaryAsync(request)
                : UAuthClient.Identifiers.SetUserPrimaryAsync(
                    UserKey.Value, request),
            "Primary identifier updated.");
    }

    private async Task UnsetPrimaryAsync(Guid id)
    {
        var request = new UnsetPrimaryUserIdentifierRequest { Id = id };

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Identifiers.UnsetMyPrimaryAsync(request)
                : UAuthClient.Identifiers.UnsetUserPrimaryAsync(
                    UserKey.Value, request),
            "Primary identifier removed.");
    }

    private async Task VerifyAsync(Guid id)
    {
        if (PreviewMode)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Development Verification",
            "This sample verification bypasses ownership proof. " +
            "Only use this operation in a development environment " +
            "with an explicitly configured demo verifier.",
            yesText: "Continue",
            noText: "Cancel");

        if (confirmed != true)
            return;

        var request = new VerifyUserIdentifierRequest
        {
            Id = id,
            Proof = UserKey is null
                ? "demo-verification-bypass"
                : null
        };

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Identifiers.VerifyMyAsync(request)
                : UAuthClient.Identifiers.VerifyUserAsync(
                    UserKey.Value, request),
            "Identifier verified.");
    }

    private async Task DeleteIdentifierAsync(Guid id)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Identifier",
            "Are you sure you want to remove this identifier?",
            yesText: "Delete",
            noText: "Cancel");

        if (confirmed != true)
            return;

        var request = new DeleteUserIdentifierRequest { Id = id };

        await ExecuteAsync(
            () => UserKey is null
                ? UAuthClient.Identifiers.DeleteMyAsync(request)
                : UAuthClient.Identifiers.DeleteUserAsync(
                    UserKey.Value, request),
            "Identifier deleted.");
    }

    private async Task<bool> ExecuteAsync(
        Func<Task<UAuthResult>> operation,
        string successMessage)
    {
        if (_loading || PreviewMode)
            return false;

        _loading = true;

        try
        {
            var result = await operation();

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Identifier operation failed.",
                    Severity.Error);
                return false;
            }

            Snackbar.Add(successMessage, Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred.",
                Severity.Error);
            return false;
        }
        finally
        {
            _loading = false;
        }

        await ReloadAsync();
        return true;
    }

    private static string GetIdentifierIcon(UserIdentifierType type)
    {
        return type switch
        {
            UserIdentifierType.Username => Icons.Material.Filled.PersonOutline,
            UserIdentifierType.Email => Icons.Material.Filled.AlternateEmail,
            UserIdentifierType.Phone => Icons.Material.Filled.Phone,
            _ => Icons.Material.Filled.Badge
        };
    }

    private void Cancel()
    {
        if (!_loading)
            MudDialog.Close(DialogResult.Ok(true));
    }
}
