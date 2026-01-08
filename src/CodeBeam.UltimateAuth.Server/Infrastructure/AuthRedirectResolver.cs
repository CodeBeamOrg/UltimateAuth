using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class AuthRedirectResolver
    {
        private readonly UAuthServerOptions _options;

        public AuthRedirectResolver(IOptions<UAuthServerOptions> options)
        {
            _options = options.Value;
        }

        // TODO: Add allowed origins validation
        public string ResolveClientBase(HttpContext ctx)
        {
            if (ctx.Request.Query.TryGetValue("returnUrl", out var returnUrl) &&
                Uri.TryCreate(returnUrl!, UriKind.Absolute, out var ru))
            {
                return ru.GetLeftPart(UriPartial.Authority);
            }

            if (ctx.Request.Headers.TryGetValue("Origin", out var origin) &&
                Uri.TryCreate(origin!, UriKind.Absolute, out var originUri))
            {
                return originUri.GetLeftPart(UriPartial.Authority);
            }

            if (ctx.Request.Headers.TryGetValue("Referer", out var referer) &&
                Uri.TryCreate(referer!, UriKind.Absolute, out var refUri))
            {
                return refUri.GetLeftPart(UriPartial.Authority);
            }

            if (!string.IsNullOrWhiteSpace(_options.Hub.ClientBaseAddress))
                return _options.Hub.ClientBaseAddress;

            return $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        }

        public string ResolveRedirect(HttpContext ctx, string path, IDictionary<string, string?>? query = null)
        {
            var url = Combine(ResolveClientBase(ctx), path);

            if (query is null || query.Count == 0)
                return url;

            var qs = string.Join("&", query
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));

            return string.IsNullOrWhiteSpace(qs)
                ? url
                : $"{url}?{qs}";
        }

        private static string Combine(string baseUri, string path)
        {
            return baseUri.TrimEnd('/') + "/" + path.TrimStart('/');
        }
    }

}
