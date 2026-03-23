using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public partial class UAuthLoginForm
{
    [Inject]
    IDeviceIdProvider DeviceIdProvider { get; set; } = null!;

    [Inject]
    IUAuthClient UAuthClient { get; set; } = null!;

    [Inject]
    IHubCredentialResolver HubCredentialResolver { get; set; } = null!;

    [Inject]
    IHubFlowReader HubFlowReader { get; set; } = null!;

    [Inject]
    IHubCapabilities HubCapabilities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique login identifier associated with this component instance.
    /// </summary>
    [Parameter]
    public string? Identifier { get; set; }

    /// <summary>
    /// Gets or sets the secret value used for authentication or configuration.
    /// </summary>
    [Parameter]
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL (login path) used to connect to the target service. If not set, endpoint is set by options.
    /// </summary>
    [Parameter]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the URL to which the user is redirected after a successful operation. If not set, return url is set by hub flow state's return url.
    /// </summary>
    [Parameter]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the associated UAuthHub flow id. If not set, value is set from query.
    /// </summary>
    [Parameter]
    public HubSessionId? HubSessionId { get; set; }

    /// <summary>
    /// Gets or sets the type of login method to use for authentication.
    /// </summary>
    [Parameter]
    public UAuthLoginType LoginType { get; set; } = UAuthLoginType.Password;

    /// <summary>
    /// Gets or sets the mode used to submit authentication requests. Default is TryAndCommit.
    /// </summary>
    [Parameter]
    public UAuthSubmitMode SubmitMode { get; set; } = UAuthSubmitMode.TryAndCommit;

    /// <summary>
    /// Gets or sets the content to be rendered inside the component.
    /// </summary>
    /// <remarks>Use this property to specify child markup or components that will be rendered within the body
    /// of this component. Typically set automatically when the component is used with child content in Razor
    /// syntax.</remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether pressing the Enter key submits the form.
    /// </summary>
    [Parameter]
    public bool AllowEnterKeyToSubmit { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback that is invoked when a try result event occurs. Does not fire on DirectCommit submit mode.
    /// </summary>
    /// <remarks>Use this property to handle the result of an authentication attempt. The callback receives an
    /// object that provides details about the outcome of the operation.</remarks>
    [Parameter]
    public EventCallback<IUAuthTryResult> OnTryResult { get; set; }


    private ElementReference _form;
    private HubCredentials? _credentials;
    private HubFlowState? _flow;
    private DeviceId? _deviceId;

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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _deviceId = await DeviceIdProvider.GetOrCreateAsync();
        StateHasChanged();
    }

    protected async Task ReloadCredentialsAsync()
    {
        if (LoginType != UAuthLoginType.Pkce)
            return;

        if (HubCredentialResolver is null || EffectiveHubSessionId is null)
            return;

        _credentials = await HubCredentialResolver.ResolveAsync(EffectiveHubSessionId.Value);
    }

    protected async Task ReloadStateAsync()
    {
        if (LoginType != UAuthLoginType.Pkce || EffectiveHubSessionId is null || HubFlowReader is null)
            return;

        _flow = await HubFlowReader.GetStateAsync(EffectiveHubSessionId.Value);
    }

    /// <summary>
    /// Asynchronously reloads credentials and state, and updates the component state.
    /// </summary>
    /// <remarks>Call this method to refresh the component's credentials and state. This method is typically
    /// used when external changes require the component to update its internal data and UI.</remarks>
    /// <returns>A task that represents the asynchronous reload operation.</returns>
    public async Task ReloadAsync()
    {
        await ReloadCredentialsAsync();
        await ReloadStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Submits the login form asynchronously using the configured authentication method.
    /// </summary>
    /// <returns>A task that represents the asynchronous submit operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the form has not been rendered. Call this method only after the OnAfterRender lifecycle event.</exception>
    public async Task SubmitAsync()
    {
        if (_form.Context is null)
            throw new InvalidOperationException("Form is not yet rendered. Call SubmitAsync after OnAfterRender.");

        if (LoginType == UAuthLoginType.Pkce)
        {
            await SubmitPkceAsync();
        }
        else
        {
            await SubmitPasswordAsync();
        }
    }

    private async Task SubmitPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(Secret))
        {
            throw new UAuthValidationException("Identifier and Secret are required.");
        }

        var request = new LoginRequest
        {
            Identifier = Identifier,
            Secret = Secret,
        };

        switch (SubmitMode)
        {
            case UAuthSubmitMode.DirectCommit:
                await JS.InvokeVoidAsync("uauth.submitForm", _form);
                break;

            case UAuthSubmitMode.TryOnly:
                {
                    var result = await UAuthClient.Flows.TryLoginAsync(request, UAuthSubmitMode.TryOnly);
                    await EmitResultAsync(result);
                    break;
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    var result = await UAuthClient.Flows.TryLoginAsync(request, UAuthSubmitMode.TryAndCommit, EffectiveReturnUrl);
                    await EmitResultAsync(result);
                    break;
                }
        }
    }

    private async Task SubmitPkceAsync()
    {
        if (_credentials is null)
            throw new InvalidOperationException("Missing PKCE credentials.");

        if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(Secret))
        {
            throw new UAuthValidationException("Identifier and Secret are required.");
        }

        var request = new PkceCompleteRequest
        {
            Identifier = Identifier,
            Secret = Secret,
            AuthorizationCode = _credentials.AuthorizationCode,
            CodeVerifier = _credentials.CodeVerifier,
            ReturnUrl = EffectiveReturnUrl ?? string.Empty,
            HubSessionId = EffectiveHubSessionId?.Value ?? string.Empty
        };

        switch (SubmitMode)
        {
            case UAuthSubmitMode.DirectCommit:
                {
                    await UAuthClient.Flows.CompletePkceLoginAsync(request);
                    break;
                }

            case UAuthSubmitMode.TryOnly:
                {
                    var result = await UAuthClient.Flows.TryCompletePkceLoginAsync(request, UAuthSubmitMode.TryOnly);
                    await EmitResultAsync(result);
                    break;
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    var result = await UAuthClient.Flows.TryCompletePkceLoginAsync(request, UAuthSubmitMode.TryAndCommit);
                    await EmitResultAsync(result);
                    break;
                }
        }
    }

    private async Task EmitResultAsync(IUAuthTryResult result)
    {
        if (OnTryResult.HasDelegate)
            await OnTryResult.InvokeAsync(result);
    }

    private string ClientProfileValue => Options.Value.ClientProfile.ToString();

    private string EffectiveEndpoint => LoginType == UAuthLoginType.Pkce
        ? Options.Value.Endpoints.PkceTryComplete
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

            var query = new List<string>();

            if (_credentials != null && EffectiveHubSessionId is not null)
            {
                query.Add($"hub={EffectiveHubSessionId}");
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                query.Add($"{UAuthConstants.Query.ReturnUrl}={Uri.EscapeDataString(returnUrl)}");
            }

            return query.Count == 0
                ? baseUrl
                : $"{baseUrl}?{string.Join("&", query)}";
        }
    }

    private string? EffectiveReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl)
        ? ReturnUrl
        : LoginType == UAuthLoginType.Pkce ? _flow?.ReturnUrl : Navigation.Uri;

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
