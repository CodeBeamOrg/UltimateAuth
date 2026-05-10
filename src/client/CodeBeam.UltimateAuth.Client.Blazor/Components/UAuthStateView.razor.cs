using CodeBeam.UltimateAuth.Core.Contracts;
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
    /// Determines how authorization conditions are evaluated.
    ///
    /// <para>
    /// <see cref="AuthorizationMatchMode.Any"/>:
    /// Any configured condition may succeed.
    /// </para>
    ///
    /// <para>
    /// <see cref="AuthorizationMatchMode.All"/>:
    /// All configured conditions and values must succeed.
    /// </para>
    ///
    /// <para>
    /// <see cref="AuthorizationMatchMode.Category"/>:
    /// At least one value from each configured category must succeed.
    /// For example:
    /// one matching role AND one matching permission.
    /// </para>
    ///
    /// Null or empty parameters are ignored.
    /// </summary>
    [Parameter]
    public AuthorizationMatchMode MatchMode { get; set; } = AuthorizationMatchMode.Category;

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

        var roleResults = _rolesParsed
            .Select(AuthState.IsInRole)
            .ToList();

        var permissionResults = _permissionsParsed
            .Select(AuthState.HasPermission)
            .ToList();

        bool? policyResult = null;

        if (!string.IsNullOrWhiteSpace(Policy))
        {
            policyResult = await EvaluatePolicyAsync();
        }

        return MatchMode switch
        {
            AuthorizationMatchMode.Any
                => EvaluateAny(roleResults, permissionResults, policyResult),

            AuthorizationMatchMode.All
                => EvaluateAll(roleResults, permissionResults, policyResult),

            AuthorizationMatchMode.Category
                => EvaluateCategory(roleResults, permissionResults, policyResult),

            _ => false
        };
    }

    private static bool EvaluateAny(IReadOnlyList<bool> roles, IReadOnlyList<bool> permissions, bool? policy)
    {
        return roles.Any(x => x) || permissions.Any(x => x) || policy == true;
    }

    private static bool EvaluateAll(IReadOnlyList<bool> roles, IReadOnlyList<bool> permissions, bool? policy)
    {
        if (roles.Count > 0 && roles.Any(x => !x))
            return false;

        if (permissions.Count > 0 && permissions.Any(x => !x))
            return false;

        if (policy.HasValue && !policy.Value)
            return false;

        return true;
    }

    private static bool EvaluateCategory(IReadOnlyList<bool> roles, IReadOnlyList<bool> permissions, bool? policy)
    {
        if (roles.Count > 0 && !roles.Any(x => x))
            return false;

        if (permissions.Count > 0 && !permissions.Any(x => x))
            return false;

        if (policy.HasValue && !policy.Value)
            return false;

        return true;
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
        return $"{Roles}|{Permissions}|{Policy}|{MatchMode}";
    }
}
