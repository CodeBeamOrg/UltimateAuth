using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public abstract class UAuthHubPageBase : UAuthReactiveComponentBase
{
    [Inject] protected IHubFlowReader HubFlowReader { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = UAuthConstants.Query.Hub)]
    public string? HubKey { get; set; }

    protected HubFlowState? HubState { get; private set; }

    protected bool IsHubAuthorized => HubState is { Exists: true, IsActive: true };

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await ReloadState();
    }

    public async Task ReloadState()
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
