using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
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
/// Provides a ready-to-use Blazor login form with native browser credential submission.
/// </summary>
/// <remarks>
/// When no child content is supplied, the component renders a complete login form automatically.<br/>
///
/// Custom forms must provide enabled inputs named
/// <c>Identifier</c> and <c>Secret</c>.
/// Credential binding to Blazor component state is not required.<br/><br/>
///
/// Do not disable credential inputs during submission, as disabled fields are excluded from native form submissions.
/// Disable the submit button instead.
/// </remarks>
public partial class UAuthLoginForm
{
    [Inject]
    IDeviceIdProvider DeviceIdProvider { get; set; } = null!;

    [Inject]
    IUAuthTokenStore TokenStore { get; set; } = null!;

    [Inject]
    IUAuthClientEvents ClientEvents { get; set; } = null!;

    [Inject]
    IHubCredentialResolver HubCredentialResolver { get; set; } = null!;

    [Inject]
    IHubFlowReader HubFlowReader { get; set; } = null!;

    [Inject]
    IHubCapabilities HubCapabilities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the login commit endpoint.
    /// When omitted, the configured endpoint is used.
    /// </summary>
    [Parameter]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the URL to return to after successful login.
    /// </summary>
    [Parameter]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the UAuthHub session associated with a PKCE login.
    /// When omitted, the session is resolved from the current URL.
    /// </summary>
    [Parameter]
    public HubSessionId? HubSessionId { get; set; }

    /// <summary>
    /// Gets or sets the login type. Defaults to password login.
    /// </summary>
    [Parameter]
    public UAuthLoginType LoginType { get; set; } = UAuthLoginType.Password;

    /// <summary>
    /// Gets or sets the submission mode.
    /// Defaults to <see cref="UAuthSubmitMode.TryAndCommit"/>.
    /// </summary>
    [Parameter]
    public UAuthSubmitMode SubmitMode { get; set; } = UAuthSubmitMode.TryAndCommit;

    /// <summary>
    /// Controls login submission transport.
    /// Auto uses effective server delivery configuration.
    /// </summary>
    [Parameter]
    public UAuthLoginTransport Transport { get; set; } = UAuthLoginTransport.Auto;

    /// <summary>
    /// Gets or sets custom form content.
    /// When omitted, a complete login form is rendered automatically.
    /// </summary>
    /// <remarks>
    /// Custom content must include enabled inputs named
    /// <c>Identifier</c> and <c>Secret</c>.
    /// Their values are read directly from the browser form.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets whether the component renders a hidden submit button for Enter-key submission in custom forms.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>.
    /// This does not prevent other submit controls from submitting the form.
    /// </remarks>
    [Parameter]
    public bool AllowEnterKeyToSubmit { get; set; } = true;

    /// <summary>
    /// Occurs when a structured login attempt result is available.
    /// Not invoked for direct commit submissions.
    /// </summary>
    [Parameter]
    public EventCallback<IUAuthTryResult> OnTryResult { get; set; }

    /// <summary>
    /// Occurs when the form submission state changes.
    /// Receives <see langword="true"/> when submission begins
    /// and <see langword="false"/> when it finishes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnSubmittingChanged { get; set; }

    /// <summary>
    /// Gets whether a login submission is currently in progress.
    /// </summary>
    public bool IsSubmitting => _isSubmitting;

    private bool _isSubmitting;
    private ElementReference _form;
    private HubCredentials? _credentials;
    private HubFlowState? _flow;
    private DeviceId? _deviceId;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (LoginType == UAuthLoginType.Pkce && !HubCapabilities.SupportsPkce)
        {
            throw new InvalidOperationException("PKCE login requires UAuthHub (Blazor Server).");
        }

        await ReloadCredentialsAsync();
        await ReloadStateAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _deviceId = await DeviceIdProvider.GetOrCreateAsync();
        StateHasChanged();
    }

    protected async Task ReloadCredentialsAsync()
    {
        if (LoginType != UAuthLoginType.Pkce || EffectiveHubSessionId is null)
            return;

        _credentials = await HubCredentialResolver.ResolveAsync(EffectiveHubSessionId.Value);
    }

    protected async Task ReloadStateAsync()
    {
        if (LoginType != UAuthLoginType.Pkce || EffectiveHubSessionId is null)
            return;

        _flow = await HubFlowReader.GetStateAsync(EffectiveHubSessionId.Value);
    }

    /// <summary>
    /// Reloads the associated UAuthHub credentials and flow state.
    /// </summary>
    /// <remarks>
    /// Intended for PKCE scenarios where the Hub context changes.
    /// Does not initiate a new PKCE authorization flow.
    /// </remarks>
    public async Task ReloadAsync()
    {
        await ReloadCredentialsAsync();
        await ReloadStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Submits the native browser login form using the configured authentication flow.
    /// </summary>
    /// <remarks>
    /// Credentials are read from native form fields without being passed as Blazor component parameters or JS interop arguments.
    ///
    /// The component must have completed its initial render.
    /// Concurrent submissions are ignored.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the form has not rendered or required PKCE credentials are unavailable.
    /// </exception>
    public async Task SubmitAsync()
    {
        if (_isSubmitting)
            return;

        if (_form.Context is null)
            throw new InvalidOperationException("Form is not yet rendered. Call SubmitAsync after OnAfterRender.");

        if (LoginType == UAuthLoginType.Pkce && _credentials is null)
            throw new InvalidOperationException("Missing PKCE credentials.");

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
                    applicationProfile = ApplicationProfileValue,
                    trustedOrigin = TrustedOrigin,
                    transport = Transport.ToString(),
                    transportUrl = BuildEndpoint(Options.Value.Endpoints.LoginTransport),
                    returnUrl = EffectiveReturnUrl,
                    isPkce = LoginType == UAuthLoginType.Pkce
                });

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("commit", out var commitElement))
            {
                var transportResponse =
                    commitElement.Deserialize<UAuthTransportResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new UAuthProtocolException("Invalid login commit response.");

                var accessTokenRequired = result.TryGetProperty("accessTokenInHeader", out var required) &&
                    required.ValueKind == JsonValueKind.True;

                var redirectUrl =
                    UAuthLoginCompletion.GetRedirectUrl(transportResponse);

                var isHubHandoff =
                    LoginType == UAuthLoginType.Pkce &&
                    EffectiveHubSessionId is not null;

                if (isHubHandoff)
                {
                    if (!transportResponse.Ok ||
                        transportResponse.Status is < 200 or >= 300)
                    {
                        throw new UAuthProtocolException(
                            "PKCE completion failed.");
                    }

                    var expectedReturnUrl = _flow?.ReturnUrl;

                    if (string.IsNullOrWhiteSpace(redirectUrl) ||
                        string.IsNullOrWhiteSpace(expectedReturnUrl) ||
                        !Uri.TryCreate(redirectUrl, UriKind.Absolute, out var target) ||
                        !Uri.TryCreate(expectedReturnUrl, UriKind.Absolute, out var expected) ||
                        target != expected ||
                        target.Scheme is not ("https" or "http") ||
                        !string.IsNullOrEmpty(target.UserInfo))
                    {
                        throw new UAuthProtocolException(
                            "Invalid PKCE handoff redirect.");
                    }

                    // The issued credentials belong to the initiating application.
                    // Do not store them in the Hub's local authentication state.
                    Navigation.NavigateTo(target.AbsoluteUri, forceLoad: true);

                    return;
                }

                await UAuthLoginCompletion.CompleteAsync(
                    transportResponse,
                    accessTokenRequired,
                    TokenStore,
                    ClientEvents);

                if (!string.IsNullOrWhiteSpace(redirectUrl))
                {
                    var target = Navigation.ToAbsoluteUri(redirectUrl);
                    var current = new Uri(Navigation.Uri);

                    if (target.Scheme != current.Scheme ||
                        target.Authority != current.Authority ||
                        !string.IsNullOrEmpty(target.UserInfo))
                    {
                        throw new UAuthProtocolException(
                            "Fetch login redirect must remain on the application origin.");
                    }

                    Navigation.NavigateTo(target.ToString(), forceLoad: false);
                }
            }

            if (SubmitMode == UAuthSubmitMode.DirectCommit)
                return;

            if (result.ValueKind != JsonValueKind.Object)
                throw new UAuthProtocolException("Invalid browser login response.");

            IUAuthTryResult parsed;

            if (result.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.Number &&
                status.GetInt32() == 429)
            {
                var retryAfter = result.TryGetProperty("retryAfter", out var retry)
                    ? retry.GetString()
                    : null;

                var delay = UAuthRateLimit.ParseRetryAfter(retryAfter, DateTimeOffset.UtcNow);

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
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                parsed = LoginType == UAuthLoginType.Pkce
                    ? result.Deserialize<TryPkceLoginResult>(options)
                        ?? throw new UAuthProtocolException("Invalid PKCE try result.")
                    : result.Deserialize<TryLoginResult>(options)
                        ?? throw new UAuthProtocolException("Invalid password try result.");
            }

            if (OnTryResult.HasDelegate)
                await OnTryResult.InvokeAsync(parsed);
        }
        catch (JSDisconnectedException)
        {
            // The circuit was disconnected before the login operation completed.
        }
        finally
        {
            _isSubmitting = false;
            await OnSubmittingChanged.InvokeAsync(false);
        }
    }

    private string ApplicationProfileValue => Options.Value.ApplicationProfile.ToString();

    private string? TrustedOrigin
    {
        get
        {
            var basePath = Options.Value.Endpoints.BasePath;

            if (Uri.TryCreate(basePath, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            {
                return uri.GetLeftPart(UriPartial.Authority);
            }

            return null;
        }
    }

    private string BuildEndpoint(string path)
    {
        return UAuthUrlBuilder.Build(Options.Value.Endpoints.BasePath, path, Options.Value.MultiTenant);
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

            if (LoginType == UAuthLoginType.Pkce && EffectiveHubSessionId is not null)
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

            if (query.TryGetValue(UAuthConstants.Query.Hub, out var value) &&
                CodeBeam.UltimateAuth.Core.Domain.HubSessionId.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
