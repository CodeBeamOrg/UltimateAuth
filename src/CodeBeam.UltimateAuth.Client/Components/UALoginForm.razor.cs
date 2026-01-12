using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
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
        public string? Endpoint { get; set; }

        [Parameter]
        public string? ReturnUrl { get; set; }

        [Parameter]
        public IUAuthHubContextAccessor? HubContextAccessor { get; set; }

        [Parameter]
        public UAuthLoginType LoginType { get; set; } = UAuthLoginType.Password;

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

        private string ClientProfileValue => CoreOptions.Value.ClientProfile.ToString();

        private string EffectiveEndpoint => LoginType == UAuthLoginType.Pkce
            ? Options.Value.Endpoints.PkceComplete
            : Options.Value.Endpoints.Login;


        private string ResolvedEndpoint
        {
            get
            {
                var loginPath = string.IsNullOrWhiteSpace(Endpoint)
                    ? EffectiveEndpoint
                    : Endpoint;

                var baseUrl = UAuthUrlBuilder.Combine(
                    Options.Value.Endpoints.Authority,
                    loginPath);

                var returnUrl = EffectiveReturnUrl;

                if (string.IsNullOrWhiteSpace(returnUrl))
                    return baseUrl;

                return $"{baseUrl}?{(HubContextAccessor != null ? "hub=" + HubContextAccessor.Current.HubSessionId + "&" : null)}returnUrl={Uri.EscapeDataString(returnUrl)}";
            }
        }

        private string EffectiveReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl)
                ? ReturnUrl
                : LoginType == UAuthLoginType.Pkce ? HubContextAccessor?.Current?.ReturnUrl ?? string.Empty : Navigation.Uri;

        private string? AuthorizationCode => HubContextAccessor?.Current?.Payload.TryGet("authorization_code", out string? v) == true
                ? v
                : null;

        private string? CodeVerifier => HubContextAccessor?.Current?.Payload.TryGet("code_verifier", out string? v) == true
                ? v
                : null;


    }
}
