using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface ISessionIdResolver
{
    ValueTask<AuthSessionId?> ResolveAsync(HttpContext context, CancellationToken ct = default);
}
