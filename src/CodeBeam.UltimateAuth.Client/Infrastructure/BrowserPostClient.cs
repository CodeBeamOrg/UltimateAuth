using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
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

        public Task NavigatePostAsync(string endpoint, IDictionary<string, string>? data = null)
        {
            return _js.InvokeVoidAsync("uauth.post", endpoint, data).AsTask();
        }

        public async Task<BrowserPostResult> BackgroundPostAsync(string endpoint)
        {
            var result = await _js.InvokeAsync<BrowserPostResult>("uauth.refresh", endpoint);
            return result;
        }

    }
}
