using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionActivityWriter<TUserId> : ISessionActivityWriter<TUserId> where TUserId : notnull
{
    private readonly UltimateAuthSessionDbContext<TUserId> _db;

    public EfCoreSessionActivityWriter(UltimateAuthSessionDbContext<TUserId> db)
    {
        _db = db;
    }

    public async Task TouchAsync(string? tenantId, ISession<TUserId> session, CancellationToken ct)
    {
        var projection = await _db.Sessions
            .SingleOrDefaultAsync(
                x => x.SessionId == session.SessionId &&
                     x.TenantId == tenantId,
                ct);

        if (projection is null)
            return;
        // TODO: Rethink architecture
        var updated = session as UAuthSession<TUserId>
            ?? throw new InvalidOperationException("EF Core ActivityWriter requires UAuthSession instance.");

        _db.Sessions.Update(updated.ToProjection());
        await _db.SaveChangesAsync(ct);
    }
}
