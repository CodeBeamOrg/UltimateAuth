using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions;

public interface ICredentialResponseWriter
{
    void Write(HttpContext context, GrantKind kind, AuthSessionId sessionId);
    void Write(HttpContext context, GrantKind kind, AccessToken accessToken);
    void Write(HttpContext context, GrantKind kind, RefreshToken refreshToken);
}
