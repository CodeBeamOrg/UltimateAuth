using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionActivityWriter<TUserId>
    {
        Task TouchAsync(string? tenantId, ISession<TUserId> session, CancellationToken ct);
    }
}
