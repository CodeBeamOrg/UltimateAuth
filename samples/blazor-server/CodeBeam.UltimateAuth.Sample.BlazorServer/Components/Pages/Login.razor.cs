using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Login
{
    private string? _username;
    private string? _password;
    private ClaimsPrincipal? _aspNetCoreState;
    private UAuthClientProductInfo? _productInfo;
    private MudTextField<string> _usernameField = default!;

    [CascadingParameter]
    public UAuthState AuthState { get; set; } = default!;

    [CascadingParameter]
    Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        Diagnostics.Changed += OnDiagnosticsChanged;
        _productInfo = ClientProductInfoProvider.Get();
    }

    private bool _shouldFocus;

    protected override void OnParametersSet()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        _shouldFocus = query["focus"] == "1";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocus)
        {
            _shouldFocus = false;
            await _usernameField.FocusAsync();
        }
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var state = await task;
        _aspNetCoreState = state.User;
        await InvokeAsync(StateHasChanged);
    }

    private void OnDiagnosticsChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task ProgrammaticLogin()
    {
        var deviceId = await DeviceIdProvider.GetOrCreateAsync();
        var request = new LoginRequest
        {
            Identifier = "admin",
            Secret = "admin",
            Device = DeviceContext.FromDeviceId(deviceId),
        };
        await UAuthClient.Flows.LoginAsync(request, "/home");
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("error", out var error))
            {
                ShowLoginError(error.ToString());
                ClearQueryString();
            }
        }
    }

    private void ShowLoginError(string code)
    {
        var message = code switch
        {
            "invalid_credentials" => "Invalid username or password.",
            "locked" => "Your account is locked.",
            "mfa_required" => "Multi-factor authentication required.",
            _ => "Login failed."
        };

        Snackbar.Add(message, Severity.Error);
    }

    private void ClearQueryString()
    {
        var uri = new Uri(Nav.Uri);
        var clean = uri.GetLeftPart(UriPartial.Path);
        Nav.NavigateTo(clean, replace: true);
    }

    public void Dispose()
    {
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
    }
}
