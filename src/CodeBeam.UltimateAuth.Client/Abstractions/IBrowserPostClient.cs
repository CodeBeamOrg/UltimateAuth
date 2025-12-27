namespace CodeBeam.UltimateAuth.Client.Abstractions
{
    public interface IBrowserPostClient
    {
        Task PostAsync(string endpoint, IDictionary<string, string>? data = null);
    }

}
