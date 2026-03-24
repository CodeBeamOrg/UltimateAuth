using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.ResourceApi;

public class ProductApiService
{
    private readonly HttpClient _http;

    public ProductApiService(HttpClient http)
    {
        _http = http;
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

    public async Task<List<Product>> GetAllAsync()
    {
        var request = CreateRequest(HttpMethod.Get, "/api/products");
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to fetch products: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<List<Product>>() ?? new();
    }

    public async Task<Product?> GetAsync(int id)
    {
        var request = CreateRequest(HttpMethod.Get, $"/api/products/{id}");
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task<Product?> CreateAsync(Product product)
    {
        var request = CreateRequest(HttpMethod.Post, "/api/products", product);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Create failed: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task<Product?> UpdateAsync(int id, Product product)
    {
        var request = CreateRequest(HttpMethod.Put, $"/api/products/{id}", product);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Update failed: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task DeleteAsync(int id)
    {
        var request = CreateRequest(HttpMethod.Delete, $"/api/products/{id}");
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Delete failed: {response.StatusCode}");
    }
}
