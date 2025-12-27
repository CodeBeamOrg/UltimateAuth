using CodeBeam.UltimateAuth.Client.Abstractions;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal sealed class BrowserPostClient : IBrowserPostClient
    {
        private readonly IJSRuntime _js;

        public BrowserPostClient(IJSRuntime js)
        {
            _js = js;
        }

        public Task PostAsync(string endpoint, IDictionary<string, string>? data = null)
        {
            return _js.InvokeVoidAsync("uauth.post", endpoint, data).AsTask();
        }
    }

}
