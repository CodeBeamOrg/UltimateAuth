using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public abstract class UAuthHubLayoutComponentBase : LayoutComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IHubFlowReader HubFlowReader { get; set; } = default!;

    protected HubFlowState? HubState { get; private set; }

    protected bool HasHub => HubState?.Exists == true;
    protected bool IsHubAuthorized => HasHub && HubState?.IsActive == true;
    protected bool IsExpired => HubState?.IsExpired == true;
    protected HubErrorCode? Error => HubState?.Error;

    private string? _lastHubKey;

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

    protected virtual string? ResolveHubKey()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue(UAuthConstants.Query.Hub, out var hubValue))
            return hubValue.ToString();

        return null;
    }
}
