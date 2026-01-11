using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.UAuthHub.Components.Pages
{
    public partial class Home
    {
        [SupplyParameterFromQuery(Name = "hub")]
        public string? HubKey { get; set; }

        private string _authorizationCode = default!;
        private string _codeVerifier = default!;

        private string? _username;
        private string? _password;

        private UALoginForm _form = null!;

        protected bool Ready;
        protected string? Error;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                //await HubContextInitializer.EnsureInitializedAsync();
            }
        }

        private async Task InitializeHubContext()
        {
            if (string.IsNullOrWhiteSpace(HubKey))
                return;

            var artifact = await AuthStore.ConsumeAsync(new AuthArtifactKey(HubKey));

            if (artifact is not HubFlowArtifact flow)
                return;

            var context = new UAuthHubContext(
                hubSessionId: flow.HubSessionId,
                flowType: flow.FlowType,
                clientProfile: flow.ClientProfile,
                tenantId: flow.TenantId,
                returnUrl: flow.ReturnUrl,
                payload: flow.Payload,
                createdAt: DateTimeOffset.UtcNow);

            HubContextAccessor.Initialize(context);

            StateHasChanged();
        }

        private async Task ProgrammaticPkceLogin()
        {
            var hub = HubContextAccessor.Current;

            if (hub is null)
                return;

            hub.Payload.TryGet("authorization_code", out string? authorizationCode);
            hub.Payload.TryGet("code_verifier", out string? codeVerifier);
            hub.Payload.TryGet("return_url", out string? returnUrl);

            var request = new PkceLoginRequest
            {
                Identifier = "Admin",
                Secret = "Password!",
                AuthorizationCode = authorizationCode ?? string.Empty,
                CodeVerifier = codeVerifier ?? string.Empty,
                ReturnUrl = hub.ReturnUrl ?? string.Empty
            };
            await UAuthClient.CompletePkceLoginAsync(request);
        }

        //protected override void OnAfterRender(bool firstRender)
        //{
        //    if (firstRender)
        //    {
        //        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        //        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        //        if (query.TryGetValue("error", out var error))
        //        {
        //            ShowLoginError(error.ToString());
        //            ClearQueryString();
        //        }
        //    }
        //}

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
