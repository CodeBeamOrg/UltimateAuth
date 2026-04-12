using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;

namespace CodeBeam.UAuth.Sample.IntWasm.Client.ResourceApi;

public class ProductApiService
{
    private readonly HttpClient _http;

    public ProductApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("resourceApi");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    public Task<UAuthResult<List<SampleProduct>>> GetAllAsync()
        => SendAsync<List<SampleProduct>>(CreateRequest(HttpMethod.Get, "/api/products"));

    public Task<UAuthResult<SampleProduct?>> GetAsync(int id)
        => SendAsync<SampleProduct?>(CreateRequest(HttpMethod.Get, $"/api/products/{id}"));

    public Task<UAuthResult<SampleProduct?>> CreateAsync(SampleProduct product)
        => SendAsync<SampleProduct?>(CreateRequest(HttpMethod.Post, $"/api/products", product));

    public Task<UAuthResult<SampleProduct?>> UpdateAsync(int id, SampleProduct product)
        => SendAsync<SampleProduct?>(CreateRequest(HttpMethod.Put, $"/api/products/{id}", product));

    public Task<UAuthResult<SampleProduct?>> DeleteAsync(int id)
        => SendAsync<SampleProduct?>(CreateRequest(HttpMethod.Delete, $"/api/products/{id}"));

    private async Task<UAuthResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        var response = await _http.SendAsync(request);

        var result = new UAuthResult<T>
        {
            Status = (int)response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode
        };

        if (response.IsSuccessStatusCode)
        {
            result.Value = await response.Content.ReadFromJsonAsync<T>();
            return result;
        }

        result.Problem = await TryReadProblem(response);
        return result;
    }

    private async Task<UAuthProblem?> TryReadProblem(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<UAuthProblem>();
        }
        catch
        {
            return new UAuthProblem
            {
                Title = response.ReasonPhrase
            };
        }
    }
}
