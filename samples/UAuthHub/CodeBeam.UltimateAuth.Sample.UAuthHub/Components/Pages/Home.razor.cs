using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using System;

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
            if (!firstRender)
                return;

            var hubKey = GetHubKeyFromQuery();

            if (hubKey is null)
            {
                await StartNewPkceAsync();
                return;
            }

            var state = await HubStateReader.GetStateAsync(hubKey);

            if (!state.Exists || !state.IsActive)
            {
                await StartNewPkceAsync();
                return;
            }
        }

        private string? GetHubKeyFromQuery()
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("hub", out var hubKey) && !string.IsNullOrWhiteSpace(hubKey))
                return hubKey;

            return null;
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

        private async Task StartNewPkceAsync()
        {
            var returnUrl = await ResolveReturnUrlAsync();
            await UAuthClient.BeginPkceAsync(returnUrl);
        }

        private async Task<string> ResolveReturnUrlAsync()
        {
            // 1) Live hub context
            var fromContext = HubContextAccessor.Current?.ReturnUrl;
            if (!string.IsNullOrWhiteSpace(fromContext))
                return fromContext;

            // 2) Query return_url (optional)
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("return_url", out var ru) && !string.IsNullOrWhiteSpace(ru))
                return ru!;

            // 3) Query hub -> store lookup
            if (query.TryGetValue("hub", out var hubKey) && !string.IsNullOrWhiteSpace(hubKey))
            {
                var artifact = await AuthStore.GetAsync(new AuthArtifactKey(hubKey!));
                if (artifact is HubFlowArtifact flow && !string.IsNullOrWhiteSpace(flow.ReturnUrl))
                    return flow.ReturnUrl!;
            }

            // 4) Config default (recommend adding to options)
            //if (!string.IsNullOrWhiteSpace(_options.Login.DefaultReturnUrl))
            //    return _options.Login.DefaultReturnUrl!;

            // 5) Final fallback (Hub URL)
            return Nav.Uri;
        }

        private void CompleteHubFlow()
        {
            HubContextAccessor.Complete();
            StateHasChanged();
        }

        private void HandleErrorFromQuery()
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("error", out var error))
            {
                Snackbar.Add("Login failed.", Severity.Error);
                Nav.NavigateTo(uri.GetLeftPart(UriPartial.Path), replace: true);
            }
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
