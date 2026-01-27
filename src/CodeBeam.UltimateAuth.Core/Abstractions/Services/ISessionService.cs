using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionService
    {
        Task RevokeAllAsync(AuthContext authContext, UserKey userKey, CancellationToken ct = default);
        Task RevokeAllExceptChainAsync(AuthContext authContext, UserKey userKey, SessionChainId exceptChainId, CancellationToken ct = default);
        Task RevokeRootAsync(AuthContext authContext, UserKey userKey, CancellationToken ct = default);
    }
}
