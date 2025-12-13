using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Issues and manages authentication sessions.
    /// </summary>
    public interface ISessionIssuer<TUserId>
    {
        Task<IssuedSession<TUserId>> IssueAsync(SessionIssueContext<TUserId> context, UAuthSessionChain<TUserId> chain, CancellationToken cancellationToken = default);
    }
}
