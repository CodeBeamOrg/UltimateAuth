using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal sealed class BrowserPostClient : IBrowserPostClient
    {
        private readonly IJSRuntime _js;
        private UAuthOptions _coreOptions;

        public BrowserPostClient(IJSRuntime js, IOptions<UAuthOptions> coreOptions)
        {
            _js = js;
            _coreOptions = coreOptions.Value;
        }

        public Task NavigatePostAsync(string endpoint, IDictionary<string, string>? data = null)
        {
            return _js.InvokeVoidAsync("uauth.post", new
            {
                url = endpoint,
                mode = "navigate",
                data = data,
                clientProfile = _coreOptions.ClientProfile.ToString()
            }).AsTask();
        }

        public async Task<BrowserPostResult> FetchPostAsync(string endpoint)
        {
            var result = await _js.InvokeAsync<BrowserPostResult>("uauth.post", new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = false,
                clientProfile = _coreOptions.ClientProfile.ToString()
            });

            return result;
        }

        public async Task<BrowserPostJsonResult<T>> FetchPostJsonAsync<T>(string endpoint)
        {
            var result = await _js.InvokeAsync<BrowserPostJsonResult<T>>("uauth.post", new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = true,
                clientProfile = _coreOptions.ClientProfile.ToString()
            });

            return result;
        }

    }
}
