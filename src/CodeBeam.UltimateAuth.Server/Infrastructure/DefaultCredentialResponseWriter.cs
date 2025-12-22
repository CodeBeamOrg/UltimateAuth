using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultCredentialResponseWriter : ICredentialResponseWriter
    {
        private readonly IUAuthCookieManager _cookieManager;

        public DefaultCredentialResponseWriter(
            IUAuthCookieManager cookieManager)
        {
            _cookieManager = cookieManager;
        }

        public void Write(HttpContext context, string value, CredentialResponseOptions options)
        {
            switch (options.Mode)
            {
                case TokenResponseMode.Cookie:
                    _cookieManager.Write(context, value);
                    break;

                case TokenResponseMode.Header:
                    WriteHeader(context, value, options);
                    break;

                case TokenResponseMode.Body:
                    // Intentionally NO-OP here.
                    // Body is composed by the endpoint response.
                    break;

                case TokenResponseMode.None:
                default:
                    break;
            }
        }

        private static void WriteHeader( HttpContext context, string value, CredentialResponseOptions options)
        {
            var headerName = options.Name ?? "Authorization";

            var formatted = options.HeaderFormat switch
            {
                HeaderTokenFormat.Bearer => $"Bearer {value}",
                HeaderTokenFormat.Raw => value,
                _ => value
            };

            context.Response.Headers[headerName] = formatted;
        }
    }
}
