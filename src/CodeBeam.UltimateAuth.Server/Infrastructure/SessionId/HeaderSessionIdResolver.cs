using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class HeaderSessionIdResolver : IInnerSessionIdResolver
{
    public string Name => "header";
    private readonly UAuthServerOptions _options;

    public HeaderSessionIdResolver(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value;
    }

    public AuthSessionId? Resolve(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(_options.SessionResolution.HeaderName, out var values))
            return null;

        var raw = values.FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!AuthSessionId.TryCreate(raw, out var sessionId))
            return null;

        return sessionId;
    }
}
