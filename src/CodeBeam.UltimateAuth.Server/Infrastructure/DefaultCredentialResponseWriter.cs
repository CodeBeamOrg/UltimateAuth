using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultCredentialResponseWriter : ICredentialResponseWriter
    {
        private readonly IUAuthCookieManager _cookieManager;
        private readonly UAuthServerOptions _server;

        public DefaultCredentialResponseWriter(
            IUAuthCookieManager cookieManager,
            IOptions<UAuthServerOptions> serverOptions)
        {
            _cookieManager = cookieManager;
            _server = serverOptions.Value;
        }

        public void Write(HttpContext context, string value, CredentialResponseOptions response)
        {
            switch (response.Mode)
            {
                case TokenResponseMode.Cookie:
                    WriteCookie(context, value, response);
                    break;

                case TokenResponseMode.Header:
                    WriteHeader(context, value, response);
                    break;

                case TokenResponseMode.Body:
                case TokenResponseMode.None:
                default:
                    break;
            }
        }

        private void WriteCookie(
            HttpContext ctx,
            string value,
            CredentialResponseOptions response)
        {
            var cookieOptions = ResolveCookieOptions(response);
            var logicalLifetime = ResolveLogicalLifetime(response);

            _cookieManager.Write(
                ctx,
                value,
                cookieOptions,
                logicalLifetime);
        }

        private TimeSpan? ResolveLogicalLifetime(CredentialResponseOptions response)
        {
            return response.TokenFormat switch
            {
                TokenFormat.Opaque when response.Name == "session"
                    => _server.Session.IdleTimeout,

                TokenFormat.Opaque when response.Name == "refresh"
                    => _server.Tokens.RefreshTokenLifetime,

                TokenFormat.Jwt
                    => _server.Tokens.AccessTokenLifetime,

                _ => null
            };
        }

        private UAuthCookieOptions ResolveCookieOptions(CredentialResponseOptions response)
        {
            return response.Name switch
            {
                "session" => _server.Cookie.Session,
                "access" => _server.Cookie.AccessToken,
                "refresh" => _server.Cookie.RefreshToken,

                _ => throw new InvalidOperationException(
                    $"Unknown cookie type: {response.Name}")
            };
        }

        private static void WriteHeader(
            HttpContext context,
            string value,
            CredentialResponseOptions options)
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
