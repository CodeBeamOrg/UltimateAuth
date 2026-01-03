using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultClientProfileReader : IClientProfileReader
    {
        private const string HeaderName = "X-UAuth-ClientProfile";

        public UAuthClientProfile Read(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var value) &&
                Enum.TryParse<UAuthClientProfile>(value, ignoreCase: true, out var profile))
            {
                return profile;
            }

            return UAuthClientProfile.WebServer;
        }
    }

}
