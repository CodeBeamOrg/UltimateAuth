using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    /// <summary>
    /// HTTP-aware session issuer used by UltimateAuth server components.
    /// Extends the core ISessionIssuer contract with HttpContext-bound
    /// operations required for cookie-based session binding.
    /// </summary>
    public interface IHttpSessionIssuer : ISessionIssuer
    {
        Task<IssuedSession> IssueLoginSessionAsync(HttpContext httpContext, AuthenticatedSessionContext context, CancellationToken ct = default);

        Task<IssuedSession> RotateSessionAsync(HttpContext httpContext, SessionRotationContext context, CancellationToken ct = default);
    }
}
