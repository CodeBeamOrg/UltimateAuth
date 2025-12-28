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
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool AllowEnterKeyToSubmit { get; set; } = true;

        private ElementReference _form;

        private string ResolvedEndpoint => string.IsNullOrWhiteSpace(Endpoint) ? "/auth/login" : Endpoint;

        public async Task SubmitAsync()
        {
            if (_form.Context is null)
                throw new InvalidOperationException("Form is not yet rendered. Call SubmitAsync after OnAfterRender.");

            await JS.InvokeVoidAsync("uauth.submitForm", _form);
        }
    }
}
