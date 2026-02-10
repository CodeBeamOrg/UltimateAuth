namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class UrlComposer
{
    public static string Combine(string baseUri, string path, IDictionary<string, string?>? query = null)
    {
        var url = baseUri.TrimEnd('/') + "/" + path.TrimStart('/');

        if (query is null || query.Count == 0)
            return url;

        var qs = string.Join("&",
            query
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));

        return string.IsNullOrWhiteSpace(qs)
            ? url
            : $"{url}?{qs}";
    }
}
