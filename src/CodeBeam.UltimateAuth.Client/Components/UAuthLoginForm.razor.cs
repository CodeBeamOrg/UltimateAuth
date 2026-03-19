using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public partial class UAuthLoginForm
{
    [Inject] IDeviceIdProvider DeviceIdProvider { get; set; } = null!;
    private DeviceId? _deviceId;

    [Inject]
    IHubCredentialResolver HubCredentialResolver { get; set; } = null!;

    [Inject]
    IHubFlowReader HubFlowReader { get; set; } = null!;

    [Inject]
    IHubCapabilities HubCapabilities { get; set; } = null!;

    [Parameter]
    public string? Identifier { get; set; }

    [Parameter]
    public string? Secret { get; set; }

    [Parameter]
    public string? Endpoint { get; set; }

    [Parameter]
    public string? ReturnUrl { get; set; }

    //[Parameter]
    //public IHubCredentialResolver? HubCredentialResolver { get; set; }

    //[Parameter]
    //public IHubFlowReader? HubFlowReader { get; set; }

    [Parameter]
    public HubSessionId? HubSessionId { get; set; }

    [Parameter]
    public UAuthLoginType LoginType { get; set; } = UAuthLoginType.Password;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool AllowEnterKeyToSubmit { get; set; } = true;

    private ElementReference _form;

    private HubCredentials? _credentials;
    private HubFlowState? _flow;
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await ReloadCredentialsAsync();
        await ReloadStateAsync();

        if (LoginType == UAuthLoginType.Pkce && !HubCapabilities.SupportsPkce)
        {
            throw new InvalidOperationException("PKCE login requires UAuthHub (Blazor Server). " +
                "PKCE is not supported in this client profile." +
                "Change LoginType to password or place this component to a server-side project.");
        }

        //if (LoginType == UAuthLoginType.Pkce && EffectiveHubSessionId is null)
        //{
        //    throw new InvalidOperationException("PKCE login requires an active Hub flow. " +
        //        "No 'hub' query parameter was found."
        //    );
        //}
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _deviceId = await DeviceIdProvider.GetOrCreateAsync();
        StateHasChanged();
    }

    public async Task ReloadCredentialsAsync()
    {
        if (LoginType != UAuthLoginType.Pkce)
            return;

        if (HubCredentialResolver is null || EffectiveHubSessionId is null)
            return;

        _credentials = await HubCredentialResolver.ResolveAsync(EffectiveHubSessionId.Value);
    }

    public async Task ReloadStateAsync()
    {
        if (LoginType != UAuthLoginType.Pkce || EffectiveHubSessionId is null || HubFlowReader is null)
            return;

        _flow = await HubFlowReader.GetStateAsync(EffectiveHubSessionId.Value);
    }

    public async Task SubmitAsync()
    {
        if (_form.Context is null)
            throw new InvalidOperationException("Form is not yet rendered. Call SubmitAsync after OnAfterRender.");

        await JS.InvokeVoidAsync("uauth.submitForm", _form);
    }

    private string ClientProfileValue => Options.Value.ClientProfile.ToString();

    private string EffectiveEndpoint => LoginType == UAuthLoginType.Pkce
        ? Options.Value.Endpoints.PkceComplete
        : Options.Value.Endpoints.Login;


    private string ResolvedEndpoint
    {
        get
        {
            var loginPath = string.IsNullOrWhiteSpace(Endpoint)
                ? EffectiveEndpoint
                : Endpoint;

            var baseUrl = UAuthUrlBuilder.Build(Options.Value.Endpoints.BasePath, loginPath, Options.Value.MultiTenant);
            var returnUrl = EffectiveReturnUrl;

            if (string.IsNullOrWhiteSpace(returnUrl))
                return baseUrl;

            return $"{baseUrl}?{(_credentials != null ? "hub=" + EffectiveHubSessionId + "&" : null)}{UAuthConstants.Query.ReturnUrl}={Uri.EscapeDataString(returnUrl)}";
        }
    }

    private string EffectiveReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl)
        ? ReturnUrl
        : LoginType == UAuthLoginType.Pkce ? _flow?.ReturnUrl ?? string.Empty : Navigation.Uri;

    private HubSessionId? EffectiveHubSessionId
    {
        get
        {
            if (HubSessionId is not null)
                return HubSessionId;

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue(UAuthConstants.Query.Hub, out var hubValue) && CodeBeam.UltimateAuth.Core.Domain.HubSessionId.TryParse(hubValue, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
