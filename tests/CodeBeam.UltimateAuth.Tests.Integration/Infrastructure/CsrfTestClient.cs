using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;

internal static class CsrfTestClient
{
    private sealed record TokenResponse(string RequestToken, string HeaderName, string FormFieldName);

    public static Task<HttpResponseMessage> PostJsonAsync<T>(
        HttpClient client,
        string url,
        T payload,
        string tokenUrl = "/auth/antiforgery/token")
    {
        return SendAsync(
            client,
            url,
            JsonContent.Create(payload),
            tokenUrl);
    }

    public static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string url,
        HttpContent? content = null,
        string tokenUrl = "/auth/antiforgery/token")
    {
        return SendAsync(client, url, content, tokenUrl);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string url,
        HttpContent? content,
        string tokenUrl)
    {
        using var tokenResponse = await client.GetAsync(tokenUrl);

        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content
            .ReadFromJsonAsync<TokenResponse>();

        if (token is null ||
            string.IsNullOrWhiteSpace(token.RequestToken) ||
            string.IsNullOrWhiteSpace(token.HeaderName))
        {
            throw new InvalidOperationException(
                "Invalid antiforgery token response.");
        }

        var antiforgeryCookie = tokenResponse.Headers
            .GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .FirstOrDefault(value =>
                value.TrimStart().StartsWith(
                    ".AspNetCore.Antiforgery.",
                    StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(antiforgeryCookie))
        {
            throw new InvalidOperationException(
                "Antiforgery cookie was not issued.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            url)
        {
            Content = content
        };

        request.Headers.TryAddWithoutValidation(
            token.HeaderName,
            token.RequestToken);

        // Preserve existing manually managed session cookies.
        var existingCookies = client.DefaultRequestHeaders.TryGetValues(
            "Cookie",
            out var values)
            ? string.Join("; ", values)
            : string.Empty;

        var cookieHeader = string.IsNullOrWhiteSpace(existingCookies)
            ? antiforgeryCookie
            : $"{existingCookies}; {antiforgeryCookie}";

        request.Headers.TryAddWithoutValidation(
            "Cookie",
            cookieHeader);

        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request, string tokenUrl = "/auth/antiforgery/token")
    {
        using var tokenResponse = await client.GetAsync(tokenUrl);
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content
            .ReadFromJsonAsync<TokenResponse>();

        if (token is null ||
            string.IsNullOrWhiteSpace(token.RequestToken) ||
            string.IsNullOrWhiteSpace(token.HeaderName))
        {
            throw new InvalidOperationException(
                "Invalid antiforgery token response.");
        }

        request.Headers.TryAddWithoutValidation(
            token.HeaderName,
            token.RequestToken);

        var antiforgeryCookie = tokenResponse.Headers
            .GetValues("Set-Cookie")
            .Select(x => x.Split(';', 2)[0])
            .First(x => x.StartsWith(
                ".AspNetCore.Antiforgery.",
                StringComparison.OrdinalIgnoreCase));

        var existingCookies =
            request.Headers.TryGetValues("Cookie", out var requestCookies)
                ? string.Join("; ", requestCookies)
                : client.DefaultRequestHeaders.TryGetValues("Cookie", out var clientCookies)
                    ? string.Join("; ", clientCookies)
                    : string.Empty;

        request.Headers.Remove("Cookie");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            string.IsNullOrWhiteSpace(existingCookies)
                ? antiforgeryCookie
                : $"{existingCookies}; {antiforgeryCookie}");

        return await client.SendAsync(request);
    }
}
