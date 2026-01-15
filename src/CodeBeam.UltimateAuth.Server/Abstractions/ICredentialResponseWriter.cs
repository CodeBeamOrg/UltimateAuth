using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    public interface ICredentialResponseWriter
    {
        void Write(HttpContext context, CredentialKind kind, AuthSessionId sessionId);
        void Write(HttpContext context, CredentialKind kind, AccessToken accessToken);
        void Write(HttpContext context, CredentialKind kind, RefreshToken refreshToken);
    }
}
