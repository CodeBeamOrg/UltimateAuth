using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Sample.Shared.ResourceApi;

public sealed class SampleProductApiService(IUAuthClient client)
{
    public Task<UAuthResult<List<SampleProduct>>> GetAllAsync()
        => client.Resource.GetAsync<List<SampleProduct>>("/api/products");

    public Task<UAuthResult<SampleProduct?>> GetAsync(int id)
        => client.Resource.GetAsync<SampleProduct?>($"/api/products/{id}");

    public Task<UAuthResult<SampleProduct?>> CreateAsync(SampleProduct product)
        => client.Resource.PostAsync<SampleProduct?>("/api/products", product);

    public Task<UAuthResult<SampleProduct?>> UpdateAsync(int id, SampleProduct product)
        => client.Resource.PutAsync<SampleProduct?>($"/api/products/{id}", product);

    public Task<UAuthResult> DeleteAsync(int id)
        => client.Resource.DeleteAsync($"/api/products/{id}");
}
