using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
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

        public async Task<BrowserPostResult> FetchPostAsync(string endpoint, IDictionary<string, string>? data = null)
        {
            var result = await _js.InvokeAsync<BrowserPostResult>("uauth.post", new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = false,
                data = data,
                clientProfile = _coreOptions.ClientProfile.ToString()
            });

            return result;
        }

        public async Task<BrowserPostRawResult> FetchPostJsonRawAsync(string endpoint, IDictionary<string, string>? data = null)
        {
            var postData = data ?? new Dictionary<string, string>();
            return await _js.InvokeAsync<BrowserPostRawResult>("uauth.post",
                new
                {
                    url = endpoint,
                    mode = "fetch",
                    expectJson = true,
                    data = postData,
                    clientProfile = _coreOptions.ClientProfile.ToString()
                });
        }


        //public async Task<BrowserPostJsonResult<T>> FetchPostJsonAsync<T>(string endpoint, IDictionary<string, string>? data = null)
        //{
        //    var result = await _js.InvokeAsync<BrowserPostJsonResult<T>>("uauth.post", new
        //    {
        //        url = endpoint,
        //        mode = "fetch",
        //        expectJson = true,
        //        data = data,
        //        clientProfile = _coreOptions.ClientProfile.ToString()
        //    });

        //    return result;
        //}

    }
}
