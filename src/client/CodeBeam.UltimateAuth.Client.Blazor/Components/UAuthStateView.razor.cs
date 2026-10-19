using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// A Blazor component that conditionally renders content based on the current UltimateAuth authentication state and authorization requirements.
/// </summary>
public partial class UAuthStateView : UAuthComponentBase
{
    private UAuthAuthorizationResult _result = UAuthAuthorizationResult.Denied;

    private bool _authorizing;
    private long _evaluationVersion;
    private UAuthAuthorizationRequirement? _currentRequirement;

    private bool _authorized => _result.IsAuthorized;
    private bool _inactive => _result.IsInactive;

    /// <summary>
    /// Gets or sets the content to render when the user is authorized. The content receives the current <see cref="UAuthState"/> as a parameter.
    /// </summary>
    [Parameter]
    public RenderFragment<UAuthState>? Authorized { get; set; }

    /// <summary>
    /// Gets or sets the content to render when the user is not authorized. This content is displayed when the user does not meet the specified authorization requirements.
    /// </summary>
    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    /// <summary>
    /// Gets or sets the content to render when the user is inactive. This content is displayed when the user's session state is not active, and the <see cref="RequireActive"/> parameter is set to true.
    /// </summary>
    [Parameter]
    public RenderFragment<UAuthState>? Inactive { get; set; }

    /// <summary>
    /// Gets or sets the content to render while the authorization evaluation is in progress. This content is displayed when the component is determining whether the user meets the specified authorization requirements.
    /// </summary>
    [Parameter]
    public RenderFragment? Authorizing { get; set; }

    /// <summary>
    /// Gets or sets the content to render regardless of the user's authorization state. This content is always displayed, and it receives the current <see cref="UAuthState"/> as a parameter.
    /// </summary>
    [Parameter]
    public RenderFragment<UAuthState>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of roles that the user must have to be considered authorized. The roles are evaluated based on the specified <see cref="MatchMode"/>.
    /// </summary>
    [Parameter]
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of permissions that the user must have to be considered authorized. The permissions are evaluated based on the specified <see cref="MatchMode"/>.
    /// </summary>
    [Parameter]
    public string? Permissions { get; set; }

    /// <summary>
    /// Gets or sets the name of a policy that the user must satisfy to be considered authorized. The policy is evaluated based on the specified <see cref="MatchMode"/>.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether the user's session state must be active for the user to be considered authorized.
    /// If set to true, the component will evaluate the user's session state and render the <see cref="Inactive"/> content if the session is not active.
    /// </summary>
    [Parameter]
    public bool RequireActive { get; set; } = true;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var requirement = CreateRequirement();

        if (_currentRequirement == requirement)
            return;

        _currentRequirement = requirement;

        await ReevaluateAuthorizationAsync(requirement);
    }

    /// <summary>
    /// Handles changes in the authentication state.
    /// This method is called when the authentication state changes, and it evaluates the current session state and authorization requirements.
    /// </summary>
    /// <param name="reason"></param>
    protected override async void HandleAuthStateChanged(UAuthStateChangeReason reason)
    {
        try
        {
            await InvokeAsync(async () =>
            {
                var requirement = _currentRequirement ?? CreateRequirement();
                await ReevaluateAuthorizationAsync(requirement);
            });
        }
        catch (ObjectDisposedException)
        {
            // The component was disposed during evaluation.
        }
    }

    private async Task ReevaluateAuthorizationAsync(UAuthAuthorizationRequirement requirement)
    {
        var version = ++_evaluationVersion;

        _authorizing = true;
        _result = UAuthAuthorizationResult.Denied;

        try
        {
            var result = await AuthorizationEvaluator.EvaluateAsync(AuthState, requirement);

            if (version != _evaluationVersion)
                return;

            _result = result;
        }
        finally
        {
            if (version == _evaluationVersion)
            {
                _authorizing = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private UAuthAuthorizationRequirement CreateRequirement()
    {
        return new UAuthAuthorizationRequirement
        {
            Roles = Roles,
            Permissions = Permissions,
            Policy = Policy,
            MatchMode = MatchMode,
            RequireActive = RequireActive
        };
    }
}
