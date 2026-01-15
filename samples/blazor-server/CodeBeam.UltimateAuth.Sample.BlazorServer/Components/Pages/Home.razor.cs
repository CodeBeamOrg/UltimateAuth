using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages
{
    public partial class Home
    {
        private string? _username;
        private string? _password;

        private UALoginForm _form = null!;

        private AuthenticationState _authState = null!;

        protected override async Task OnInitializedAsync()
        {
            Diagnostics.Changed += OnDiagnosticsChanged;
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
                Identifier = "Admin",
                Secret = "Password!",
                Device = new DeviceContext { DeviceId = device },
            };
            await UAuthClient.LoginAsync(request);
            _authState = await AuthStateProvider.GetAuthenticationStateAsync();
        }

        private async Task ValidateAsync()
        {
            var result = await UAuthClient.ValidateAsync();

            Snackbar.Add(
                result.IsValid ? "Session is valid ✅" : $"Session invalid ❌ ({result.State})",
                result.IsValid ? Severity.Success : Severity.Error);
        }

        private async Task LogoutAsync()
        {
            await UAuthClient.LogoutAsync();
            Snackbar.Add("Logged out", Severity.Success);
        }

        private async Task RefreshAsync()
        {
            await UAuthClient.RefreshAsync();
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
