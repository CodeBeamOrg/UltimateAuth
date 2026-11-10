
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// A login form that reads credentials directly from browser form fields.
/// Credentials are never passed through component parameters or JS interop.
/// </summary>
public partial class UAuthLoginForm
{
    [Inject]
    IDeviceIdProvider DeviceIdProvider { get; set; } = null!;

    [Inject]
    IHubCredentialResolver HubCredentialResolver { get; set; } = null!;

    [Inject]
    IHubFlowReader HubFlowReader { get; set; } = null!;

    [Inject]
    IHubCapabilities HubCapabilities { get; set; } = null!;

    [Parameter]
    public string? Endpoint { get; set; }

    [Parameter]
    public string? ReturnUrl { get; set; }

    [Parameter]
    public HubSessionId? HubSessionId { get; set; }

    [Parameter]
    public UAuthLoginType LoginType { get; set; } =
        UAuthLoginType.Password;

    [Parameter]
    public UAuthSubmitMode SubmitMode { get; set; } =
        UAuthSubmitMode.TryAndCommit;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool AllowEnterKeyToSubmit { get; set; } = true;

    [Parameter]
    public EventCallback<IUAuthTryResult> OnTryResult { get; set; }

    [Parameter]
    public EventCallback<bool> OnSubmittingChanged { get; set; }

    public bool IsSubmitting => _isSubmitting;

    private bool _isSubmitting;
    private ElementReference _form;
    private HubCredentials? _credentials;
    private HubFlowState? _flow;
    private DeviceId? _deviceId;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (LoginType == UAuthLoginType.Pkce &&
            !HubCapabilities.SupportsPkce)
        {
            throw new InvalidOperationException(
                "PKCE login requires UAuthHub (Blazor Server).");
        }

        await ReloadCredentialsAsync();
        await ReloadStateAsync();
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
        if (LoginType != UAuthLoginType.Pkce ||
            EffectiveHubSessionId is null)
            return;

        _credentials = await HubCredentialResolver.ResolveAsync(
            EffectiveHubSessionId.Value);
    }

    protected async Task ReloadStateAsync()
    {
        if (LoginType != UAuthLoginType.Pkce ||
            EffectiveHubSessionId is null)
            return;

        _flow = await HubFlowReader.GetStateAsync(
            EffectiveHubSessionId.Value);
    }

    public async Task ReloadAsync()
    {
        await ReloadCredentialsAsync();
        await ReloadStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Submits the native browser form without transferring credentials
    /// into the Blazor component or through JS interop arguments.
    /// </summary>

    public async Task SubmitAsync()
    {
        if (_isSubmitting)
            return;

        if (_form.Context is null)
            throw new InvalidOperationException(
                "Form is not yet rendered. Call SubmitAsync after OnAfterRender.");

        if (LoginType == UAuthLoginType.Pkce &&
            _credentials is null)
            throw new InvalidOperationException(
                "Missing PKCE credentials.");

        _isSubmitting = true;

        try
        {
            await OnSubmittingChanged.InvokeAsync(true);

            var result = await JS.InvokeAsync<JsonElement>(
                "uauth.submitLoginForm",
                _form,
                new
                {
                    mode = SubmitMode.ToString(),
                    tryUrl = ResolvedTryEndpoint,
                    commitUrl = ResolvedCommitEndpoint,
                    clientProfile = ClientProfileValue,
                    trustedOrigin = TrustedOrigin,
                    isPkce = LoginType == UAuthLoginType.Pkce
                });

            if (SubmitMode == UAuthSubmitMode.DirectCommit)
                return;

            if (result.ValueKind != JsonValueKind.Object)
                throw new UAuthProtocolException(
                    "Invalid browser login response.");

            IUAuthTryResult parsed;

            if (result.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.Number &&
                status.GetInt32() == 429)
            {
                var retryAfter = result.TryGetProperty(
                    "retryAfter", out var retry)
                    ? retry.GetString()
                    : null;

                var delay = UAuthRateLimit.ParseRetryAfter(
                    retryAfter, DateTimeOffset.UtcNow);

                parsed = LoginType == UAuthLoginType.Pkce
                    ? new TryPkceLoginResult
                    {
                        Status = 429,
                        ErrorCode = "rate_limited",
                        RetryAfter = delay
                    }
                    : new TryLoginResult
                    {
                        Status = 429,
                        ErrorCode = "rate_limited",
                        RetryAfter = delay
                    };
            }
            else
            {
                var options = new JsonSerializerOptions(
                    JsonSerializerDefaults.Web);

                parsed = LoginType == UAuthLoginType.Pkce
                    ? result.Deserialize<TryPkceLoginResult>(options)
                        ?? throw new UAuthProtocolException(
                            "Invalid PKCE try result.")
                    : result.Deserialize<TryLoginResult>(options)
                        ?? throw new UAuthProtocolException(
                            "Invalid password try result.");
            }

            if (OnTryResult.HasDelegate)
                await OnTryResult.InvokeAsync(parsed);
        }
        finally
        {
            _isSubmitting = false;
            await OnSubmittingChanged.InvokeAsync(false);
        }
    }

    private string ClientProfileValue =>
        Options.Value.ClientProfile.ToString();

    private string? TrustedOrigin
    {
        get
        {
            var basePath = Options.Value.Endpoints.BasePath;

            if (Uri.TryCreate(
                    basePath,
                    UriKind.Absolute,
                    out var uri) &&
                uri.Scheme is "http" or "https")
            {
                return uri.GetLeftPart(UriPartial.Authority);
            }

            return null;
        }
    }

    private string BuildEndpoint(string path)
    {
        return UAuthUrlBuilder.Build(
            Options.Value.Endpoints.BasePath,
            path,
            Options.Value.MultiTenant);
    }

    private string ResolvedTryEndpoint =>
        BuildEndpoint(
            LoginType == UAuthLoginType.Pkce
                ? Options.Value.Endpoints.PkceTryComplete
                : Options.Value.Endpoints.TryLogin);

    private string ResolvedCommitEndpoint
    {
        get
        {
            var path = !string.IsNullOrWhiteSpace(Endpoint)
                ? Endpoint
                : LoginType == UAuthLoginType.Pkce
                    ? Options.Value.Endpoints.PkceComplete
                    : Options.Value.Endpoints.Login;

            var url = BuildEndpoint(path);

            var query = new List<string>();

            if (LoginType == UAuthLoginType.Pkce &&
                EffectiveHubSessionId is not null)
            {
                query.Add(
                    $"{UAuthConstants.Query.Hub}=" +
                    Uri.EscapeDataString(
                        EffectiveHubSessionId.Value.Value));
            }

            if (!string.IsNullOrWhiteSpace(EffectiveReturnUrl))
            {
                query.Add(
                    $"{UAuthConstants.Query.ReturnUrl}=" +
                    Uri.EscapeDataString(EffectiveReturnUrl));
            }

            return query.Count == 0
                ? url
                : $"{url}{(url.Contains('?') ? "&" : "?")}{string.Join("&", query)}";
        }
    }

    private string? EffectiveReturnUrl =>
        !string.IsNullOrWhiteSpace(ReturnUrl)
            ? ReturnUrl
            : LoginType == UAuthLoginType.Pkce
                ? _flow?.ReturnUrl
                : Navigation.Uri;

    private HubSessionId? EffectiveHubSessionId
    {
        get
        {
            if (HubSessionId is not null)
                return HubSessionId;

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue(
                    UAuthConstants.Query.Hub,
                    out var value) &&
                CodeBeam.UltimateAuth.Core.Domain.HubSessionId.TryParse(
                    value,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
