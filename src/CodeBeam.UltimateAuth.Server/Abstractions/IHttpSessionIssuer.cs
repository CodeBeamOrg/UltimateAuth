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
    public interface IHttpSessionIssuer<TUserId> : ISessionIssuer<TUserId>
    {
        Task<IssuedSession<TUserId>> IssueLoginSessionAsync(HttpContext httpContext, AuthenticatedSessionContext<TUserId> context, CancellationToken cancellationToken = default);

        Task<IssuedSession<TUserId>> RotateSessionAsync(HttpContext httpContext, SessionRotationContext<TUserId> context, CancellationToken cancellationToken = default);
    }
}
