using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Pages
{
    public partial class Home
    {
        [CascadingParameter]
        public UAuthState Auth { get; set; }

        private string? _username;
        private string? _password;

        private UALoginForm _form = null!;

        private AuthenticationState _authState = null!;

        protected override async Task OnInitializedAsync()
        {
            Diagnostics.Changed += OnDiagnosticsChanged;
            //_authState = await AuthStateProvider.GetAuthenticationStateAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await StateManager.EnsureAsync();
                _authState = await AuthStateProvider.GetAuthenticationStateAsync();
                StateHasChanged();
            }
        }

        private void OnDiagnosticsChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task ProgrammaticLogin()
        {
            var device = await DeviceIdProvider.GetOrCreateAsync();
            var request = new LoginRequest
            {
                Identifier = "admin",
                Secret = "admin",
                Device = DeviceContext.FromDeviceId(device),
            };
            await UAuthClient.Flows.LoginAsync(request);
        }

        private async Task StartPkceLogin()
        {
            await UAuthClient.Flows.BeginPkceAsync();
            //await UAuthClient.NavigateToHubLoginAsync(Nav.Uri);
        }

        private async Task ValidateAsync()
        {
            var result = await UAuthClient.Flows.ValidateAsync();

            Snackbar.Add(
                result.IsValid ? "Session is valid ✅" : $"Session invalid ❌ ({result.State})",
                result.IsValid ? Severity.Success : Severity.Error);
        }

        private async Task LogoutAsync()
        {
            await UAuthClient.Flows.LogoutAsync();
            Snackbar.Add("Logged out", Severity.Success);
        }

        private async Task RefreshAsync()
        {
            await UAuthClient.Flows.RefreshAsync();
        }

        private async Task RefreshAuthState()
        {
            await StateManager.OnLoginAsync();
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
                "invalid" => "Invalid username or password.",
                "locked" => "Your account is locked.",
                "mfa" => "Multi-factor authentication required.",
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
            Diagnostics.Changed -= OnDiagnosticsChanged;
        }

    }
}
