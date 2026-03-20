using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

internal static class SessionChainIdEfConverter
{
    public static SessionChainId FromDatabase(Guid raw)
    {
        return SessionChainId.From(raw);
    }

    public static Guid ToDatabase(SessionChainId id) => id.Value;

    public static SessionChainId? FromDatabaseNullable(Guid? raw)
    {
        if (raw is null)
            return null;

        return SessionChainId.From(raw.Value);
    }

    public static Guid? ToDatabaseNullable(SessionChainId? id) => id?.Value;
}
