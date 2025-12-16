using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Issues and manages authentication sessions.
    /// </summary>
    public interface ISessionIssuer<TUserId>
    {
        Task<IssuedSession<TUserId>> IssueAsync(AuthenticatedSessionContext<TUserId> context, ISessionChain<TUserId> chain, CancellationToken cancellationToken = default);
    }
}
