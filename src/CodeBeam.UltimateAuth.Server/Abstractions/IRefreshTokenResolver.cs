using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions;

public interface IRefreshTokenResolver
{
    /// <summary>
    /// Resolves refresh token from incoming HTTP request.
    /// Returns null if no refresh token is present.
    /// </summary>
    string? Resolve(HttpContext context);
}
