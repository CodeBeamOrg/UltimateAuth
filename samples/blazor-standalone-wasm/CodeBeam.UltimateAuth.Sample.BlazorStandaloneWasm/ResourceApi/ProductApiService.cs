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

    public async Task<List<Product>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to fetch products: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<List<Product>>() ?? new();
    }

    public async Task<Product?> GetAsync(int id)
    {
        var response = await _http.GetAsync($"/api/products/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task<Product?> CreateAsync(Product product)
    {
        var response = await _http.PostAsJsonAsync("/api/products", product);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Create failed: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"/api/products/{id}");

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Delete failed: {response.StatusCode}");
    }
}
