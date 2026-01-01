using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Abstractions
{
    public interface IBrowserPostClient
    {
        /// <summary>
        /// Sends a POST request to the specified endpoint with the provided form data and navigates to the resulting. Submits a form.
        /// location asynchronously.
        /// </summary>
        /// <param name="endpoint">The relative or absolute URI of the endpoint to which the POST request is sent. Cannot be null or empty.</param>
        /// <param name="data">An optional collection of key-value pairs representing form data to include in the POST request. If null, no
        /// form data is sent.</param>
        /// <returns>A task that represents the asynchronous navigation operation.</returns>
        Task NavigatePostAsync(string endpoint, IDictionary<string, string>? data = null);

        /// <summary>
        /// Background POST request with JS fetch.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task<BrowserPostResult> BackgroundPostAsync(string endpoint);

        Task<BrowserPostJsonResult<T>> BackgroundPostJsonAsync<T>(string url);
    }
}
