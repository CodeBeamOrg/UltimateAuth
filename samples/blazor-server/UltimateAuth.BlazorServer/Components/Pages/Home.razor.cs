using CodeBeam.UltimateAuth.Client.Components;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using MudBlazor;

namespace UltimateAuth.BlazorServer.Components.Pages
{
    public partial class Home
    {
        private string? _username;
        private string? _password;

        private UALoginForm _form;

        private async Task HandleLogin()
        {
            await _form.SubmitAsync();
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


    }
}
