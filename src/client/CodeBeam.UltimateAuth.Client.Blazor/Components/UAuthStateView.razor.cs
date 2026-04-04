using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public partial class UAuthStateView : UAuthReactiveComponentBase
{
    private IReadOnlyList<string> _rolesParsed = Array.Empty<string>();
    private IReadOnlyList<string> _permissionsParsed = Array.Empty<string>();
    private bool _authorized;
    private bool _inactive;
    private bool _authorizing;
    private string? _authKey;
    private string? _rolesRaw;
    private string? _permissionsRaw;

    [Parameter]
    public RenderFragment<UAuthState>? Authorized { get; set; }

    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    [Parameter]
    public RenderFragment<UAuthState>? Inactive { get; set; }

    [Parameter]
    public RenderFragment? Authorizing { get; set; }

    [Parameter]
    public RenderFragment<UAuthState>? ChildContent { get; set; }

    [Parameter]
    public string? Roles { get; set; }

    [Parameter]
    public string? Permissions { get; set; }

    [Parameter]
    public string? Policy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all set conditions must be matched for the operation to succeed.
    /// Null parameters don't count as condition.
    /// </summary>
    [Parameter]
    public bool MatchAll { get; set; } = true;

    [Parameter]
    public bool RequireActive { get; set; } = true;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var newKey = BuildAuthKey();

        if (_authKey == newKey)
            return;

        _authKey = newKey;
        _authorizing = true;

        if (_rolesRaw != Roles)
        {
            _rolesRaw = Roles;
            _rolesParsed = ParseCsv(Roles);
        }

        if (_permissionsRaw != Permissions)
        {
            _permissionsRaw = Permissions;
            _permissionsParsed = ParseCsv(Permissions);
        }

        EvaluateSessionState();
        _authorized = await EvaluateAuthorizationAsync();
        _authorizing = false;
    }

    protected override async void HandleAuthStateChanged(UAuthStateChangeReason reason)
    {
        EvaluateSessionState();
        _authorizing = true;
        _authorized = await EvaluateAuthorizationAsync();
        _authorizing = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> EvaluateAuthorizationAsync()
    {
        if (!AuthState.IsAuthenticated)
            return false;

        var roles = _rolesParsed;
        var permissions = _permissionsParsed;

        var results = new List<bool>();

        if (roles.Count > 0)
        {
            results.Add(MatchAll
                ? roles.All(AuthState.IsInRole)
                : roles.Any(AuthState.IsInRole));
        }

        if (permissions.Count > 0)
        {
            results.Add(MatchAll
                ? permissions.All(AuthState.HasPermission)
                : permissions.Any(AuthState.HasPermission));
        }

        if (!string.IsNullOrWhiteSpace(Policy))
            results.Add(await EvaluatePolicyAsync());

        if (results.Count == 0)
            return true;

        return MatchAll
            ? results.All(x => x)
            : results.Any(x => x);
    }

    private void EvaluateSessionState()
    {
        if (!RequireActive)
        {
            _inactive = false;
            return;
        }

        if (AuthState.IsAuthenticated != true)
        {
            _inactive = false;
            return;
        }

        if (AuthState.Identity?.SessionState is null)
        {
            _inactive = false;
        }
        else
        {
            _inactive = AuthState.Identity?.SessionState != SessionState.Active;
        }
    }

    private static IReadOnlyList<string> ParseCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToArray();
    }

    private async Task<bool> EvaluatePolicyAsync()
    {
        if (string.IsNullOrWhiteSpace(Policy))
            return true;

        var principal = AuthState.ToClaimsPrincipal();
        var result = await AuthorizationService.AuthorizeAsync(principal, Policy);

        return result.Succeeded;
    }

    private string BuildAuthKey()
    {
        return $"{Roles}|{Permissions}|{Policy}|{MatchAll}";
    }
}
