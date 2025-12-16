using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class QuerySessionIdResolver : ISessionIdResolver
    {
        private readonly string _parameterName;

        public QuerySessionIdResolver(string parameterName)
        {
            _parameterName = parameterName;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            if (!context.Request.Query.TryGetValue(_parameterName, out var values))
                return null;

            var raw = values.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return new AuthSessionId(raw.Trim());
        }
    }
}
