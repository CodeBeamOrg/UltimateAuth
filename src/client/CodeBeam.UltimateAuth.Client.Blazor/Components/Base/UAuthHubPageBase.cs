using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Base class for Blazor pages that participate in an UltimateAuth Hub flow.
/// </summary>
/// <remarks>
/// <para>
/// The page receives the Hub session identifier from the current query string
/// and exposes the corresponding <see cref="HubFlowState"/> to derived pages.
/// </para>
/// <para>
/// Hub state is obtained through <see cref="IHubFlowReader"/> and is automatically
/// loaded when component parameters are processed. Derived pages can explicitly
/// refresh the state by calling <see cref="ReloadStateAsync"/>.
/// </para>
/// <para>
/// This type provides Hub flow state to the UI and does not itself authorize,
/// complete, or otherwise make security decisions for the authentication flow.
/// </para>
/// </remarks>
public abstract class UAuthHubPageBase : UAuthComponentBase
{
    /// <summary>
    /// Gets the Hub flow reader used to retrieve the state of the current Hub session.
    /// </summary>
    [Inject] protected IHubFlowReader HubFlowReader { get; set; } = default!;

    /// <summary>
    /// Gets or sets the raw Hub session identifier supplied by the current query string.
    /// </summary>
    /// <remarks>
    /// The value is supplied from <see cref="UAuthConstants.Query.Hub"/>.
    /// It is validated as a <see cref="HubSessionId"/> before Hub state is read.
    /// </remarks>
    [Parameter]
    [SupplyParameterFromQuery(Name = UAuthConstants.Query.Hub)]
    public string? HubKey { get; set; }

    /// <summary>
    /// Gets the state associated with the current Hub session, when one can be resolved.
    /// </summary>
    /// <remarks>
    /// The value is <see langword="null"/> when no Hub session identifier is supplied
    /// or when the supplied identifier is invalid.
    /// </remarks>
    protected HubFlowState? HubState { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the resolved Hub flow exists and is active.
    /// </summary>
    protected bool IsHubActive => HubState is { Exists: true, IsActive: true };

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await ReloadStateAsync();
    }

    /// <summary>
    /// Reloads the Hub flow state associated with the current <see cref="HubKey"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="HubKey"/> is missing, empty, or invalid, <see cref="HubState"/> is cleared.
    /// </para>
    /// <para>
    /// A valid Hub session identifier is resolved through <see cref="IHubFlowReader"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A task that represents the asynchronous reload operation.
    /// </returns>
    public async Task ReloadStateAsync()
    {
        if (string.IsNullOrWhiteSpace(HubKey))
        {
            HubState = null;
            return;
        }

        if (HubSessionId.TryParse(HubKey, out var id))
        {
            HubState = await HubFlowReader.GetStateAsync(id);
        }
        else
        {
            HubState = null;
        }
    }
}
