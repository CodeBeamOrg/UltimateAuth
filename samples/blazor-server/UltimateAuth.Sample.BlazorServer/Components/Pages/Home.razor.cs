using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using MudBlazor;

namespace UltimateAuth.Sample.BlazorServer.Components.Pages
{
    public partial class Home
    {
        private string? _username;
        private string? _password;

        private UALoginForm _form = null!;

        private async Task ProgrammaticLogin()
        {
            var request = new LoginRequest
            {
                Identifier = "Admin",
                Secret = "Password!",
            };
            await UAuthClient.LoginAsync(request);
        }

        private async Task ValidateAsync()
        {
            var httpContext = HttpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                Snackbar.Add("HttpContext not available", Severity.Error);
                return;
            }

            var credential = CredentialResolver.Resolve(httpContext);

            if (credential is null)
            {
                Snackbar.Add("No credential found", Severity.Error);
                return;
            }

            if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            {
                Snackbar.Add("Invalid session id", Severity.Error);
                return;
            }

            var result = await SessionQuery.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = credential.TenantId,
                    SessionId = sessionId,
                    Device = credential.Device,
                    Now = Clock.UtcNow
                });

            if (result.IsValid)
            {
                Snackbar.Add("Session is valid ✅", Severity.Success);
            }
            else
            {
                Snackbar.Add(
                    $"Session invalid ❌ ({result.State})",
                    Severity.Error);
            }
        }

        private async Task LogoutAsync()
        {
            await UAuthClient.LogoutAsync();
            Snackbar.Add("Logged out", Severity.Success);
        }

        private async Task RefreshAsync()
        {
            await UAuthClient.RefreshAsync();
            //Snackbar.Add("Logged out", Severity.Success);
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

    }
}
