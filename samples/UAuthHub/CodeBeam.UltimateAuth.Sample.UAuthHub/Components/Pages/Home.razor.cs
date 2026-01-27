using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Utilities;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.UAuthHub.Components.Pages
{
    public partial class Home
    {
        [SupplyParameterFromQuery(Name = "hub")]
        public string? HubKey { get; set; }

        private string? _username;
        private string? _password;

        private HubFlowState? _state;

        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrWhiteSpace(HubKey))
            {
                _state = null;
                return;
            }

            _state = await HubFlowReader.GetStateAsync(new HubSessionId(HubKey));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            var currentError = await BrowserStorage.GetAsync(StorageScope.Session, "uauth:last_error");

            if (!string.IsNullOrWhiteSpace(currentError))
            {
                Snackbar.Add(ResolveErrorMessage(currentError), Severity.Error);
                await BrowserStorage.RemoveAsync(StorageScope.Session, "uauth:last_error");
            }

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("__uauth_error", out var error))
            {
                await BrowserStorage.SetAsync(StorageScope.Session, "uauth:last_error", error.ToString());
            }
            
            if (string.IsNullOrWhiteSpace(HubKey))
            {
                return;
            }

            if (_state is null || !_state.Exists)
                return;

            if (_state?.IsActive != true)
            {
                await StartNewPkceAsync();
                return;
            }
        }

        // For testing & debugging
        private async Task ProgrammaticPkceLogin()
        {
            var hub = _state;

            if (hub is null)
                return;

            var credentials = await HubCredentialResolver.ResolveAsync(new HubSessionId(HubKey));

            var request = new PkceLoginRequest
            {
                Identifier = "admin",
                Secret = "admin",
                AuthorizationCode = credentials?.AuthorizationCode ?? string.Empty,
                CodeVerifier = credentials?.CodeVerifier ?? string.Empty,
                ReturnUrl = _state?.ReturnUrl ?? string.Empty
            };
            await UAuthClient.Flows.CompletePkceLoginAsync(request);
        }

        private async Task StartNewPkceAsync()
        {
            var returnUrl = await ResolveReturnUrlAsync();
            await UAuthClient.Flows.BeginPkceAsync(returnUrl);
        }

        private async Task<string> ResolveReturnUrlAsync()
        {
            var fromContext = _state?.ReturnUrl;
            if (!string.IsNullOrWhiteSpace(fromContext))
                return fromContext;

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("return_url", out var ru) && !string.IsNullOrWhiteSpace(ru))
                return ru!;

            if (query.TryGetValue("hub", out var hubKey) && !string.IsNullOrWhiteSpace(hubKey))
            {
                var artifact = await AuthStore.GetAsync(new AuthArtifactKey(hubKey!));
                if (artifact is HubFlowArtifact flow && !string.IsNullOrWhiteSpace(flow.ReturnUrl))
                    return flow.ReturnUrl!;
            }

            // Config default (recommend adding to options)
            //if (!string.IsNullOrWhiteSpace(_options.Login.DefaultReturnUrl))
            //    return _options.Login.DefaultReturnUrl!;

            return Nav.Uri;
        }
        
        private string ResolveErrorMessage(string? errorKey)
        {
            if (errorKey == "invalid")
            {
                return "Login failed.";
            }

            return "Failed attempt.";
        }

    }
}
