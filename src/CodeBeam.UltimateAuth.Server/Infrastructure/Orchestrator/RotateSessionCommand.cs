using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed record RotateSessionCommand<TUserId>(SessionRotationContext<TUserId> RotationContext) : ISessionCommand<TUserId, IssuedSession<TUserId>>
    {
        public Task<IssuedSession<TUserId>> ExecuteAsync(AuthContext _, ISessionIssuer<TUserId> issuer, CancellationToken ct)
        {
            return issuer.RotateSessionAsync(RotationContext, ct);
        }
    }
}
