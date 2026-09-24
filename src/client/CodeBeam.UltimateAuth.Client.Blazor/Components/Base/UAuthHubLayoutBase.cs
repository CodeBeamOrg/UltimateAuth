using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Base class for Blazor layouts that participate in an UltimateAuth Hub flow.
/// </summary>
/// <remarks>
/// <para>
/// The layout resolves the Hub session identifier from the current navigation URI
/// and exposes the corresponding <see cref="HubFlowState"/> to derived layouts.
/// </para>
/// <para>
/// Hub state is obtained through <see cref="IHubFlowReader"/>. An absent or invalid
/// Hub session identifier results in no current Hub state.
/// </para>
/// <para>
/// This type provides Hub flow state to the UI and does not itself authorize,
/// complete, or otherwise make security decisions for the authentication flow.
/// </para>
/// </remarks>
public abstract class UAuthHubLayoutBase : LayoutComponentBase
{
    /// <summary>
    /// Gets the navigation service used to inspect the current URI.
    /// </summary>
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets the Hub flow reader used to retrieve the state of the current Hub session.
    /// </summary>
    [Inject] protected IHubFlowReader HubFlowReader { get; set; } = default!;

    /// <summary>
    /// Gets the state associated with the current Hub session, when one can be resolved.
    /// </summary>
    /// <remarks>
    /// The value is <see langword="null"/> when the current URI does not contain a Hub
    /// session identifier, the identifier is invalid, or no state has been loaded.
    /// </remarks>
    protected HubFlowState? HubState { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the resolved Hub flow exists.
    /// </summary>
    protected bool HasHub => HubState?.Exists == true;

    /// <summary>
    /// Gets a value indicating whether the resolved Hub flow exists and is active.
    /// </summary>
    protected bool IsHubActive => HasHub && HubState?.IsActive == true;

    /// <summary>
    /// Gets a value indicating whether the resolved Hub flow has expired.
    /// </summary>
    protected bool IsExpired => HubState?.IsExpired == true;

    /// <summary>
    /// Gets the error associated with the resolved Hub flow, if any.
    /// </summary>
    protected HubErrorCode? Error => HubState?.Error;

    private string? _lastHubKey;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var hubKey = ResolveHubKey();

        if (string.IsNullOrWhiteSpace(hubKey))
        {
            HubState = null;
            return;
        }

        if (_lastHubKey == hubKey && HubState is not null)
            return;

        _lastHubKey = hubKey;

        if (HubSessionId.TryParse(hubKey, out var hubId))
        {
            HubState = await HubFlowReader.GetStateAsync(hubId);
        }
        else
        {
            HubState = null;
        }
    }

    /// <summary>
    /// Resolves the Hub session identifier associated with the current navigation URI.
    /// </summary>
    /// <returns>
    /// The raw Hub session identifier when present; otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// The default implementation reads <see cref="UAuthConstants.Query.Hub"/> from
    /// the current query string. Derived layouts may override this method to provide
    /// the Hub session identifier from another source.
    /// </remarks>
    protected virtual string? ResolveHubKey()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue(UAuthConstants.Query.Hub, out var hubValue))
            return hubValue.ToString();

        return null;
    }
}
