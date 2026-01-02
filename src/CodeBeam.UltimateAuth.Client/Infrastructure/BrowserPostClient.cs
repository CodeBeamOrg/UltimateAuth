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
            return _js.InvokeVoidAsync("uauth.post", new
            {
                url = endpoint,
                mode = "navigate",
                data = data
            }).AsTask();
        }

        public async Task<BrowserPostResult> FetchPostAsync(string endpoint)
        {
            var result = await _js.InvokeAsync<BrowserPostResult>("uauth.post", new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = false
            });

            return result;
        }

        public async Task<BrowserPostJsonResult<T>> FetchPostJsonAsync<T>(string endpoint)
        {
            var result = await _js.InvokeAsync<BrowserPostJsonResult<T>>("uauth.post", new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = true
            });

            return result;
        }

    }
}
