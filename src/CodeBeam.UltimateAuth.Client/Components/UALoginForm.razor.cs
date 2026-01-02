using CodeBeam.UltimateAuth.Client.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client
{
    public partial class UALoginForm
    {
        [Parameter]
        public string? Identifier { get; set; }

        [Parameter]
        public string? Secret { get; set; }

        [Parameter]
        public string? Endpoint { get; set; } = "/auth/login";

        [Parameter]
        public string? ReturnUrl { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool AllowEnterKeyToSubmit { get; set; } = true;

        private ElementReference _form;

        public async Task SubmitAsync()
        {
            if (_form.Context is null)
                throw new InvalidOperationException("Form is not yet rendered. Call SubmitAsync after OnAfterRender.");

            await JS.InvokeVoidAsync("uauth.submitForm", _form);
        }

        //private string ResolvedEndpoint => string.IsNullOrWhiteSpace(Endpoint) ? UAuthUrlBuilder.Combine(Options.Value.Endpoints.Authority, "/auth/login") : UAuthUrlBuilder.Combine(Options.Value.Endpoints.Authority, Endpoint);

        private string ResolvedEndpoint
        {
            get
            {
                var loginPath = string.IsNullOrWhiteSpace(Endpoint)
                    ? Options.Value.Endpoints.Login
                    : Endpoint;

                var baseUrl = UAuthUrlBuilder.Combine(
                    Options.Value.Endpoints.Authority,
                    loginPath);

                var returnUrl = EffectiveReturnUrl;

                if (string.IsNullOrWhiteSpace(returnUrl))
                    return baseUrl;

                return $"{baseUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }
        }

        private string EffectiveReturnUrl =>
            !string.IsNullOrWhiteSpace(ReturnUrl)
                ? ReturnUrl
                : Navigation.Uri;

    }
}
