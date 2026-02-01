using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal static class AuthSessionIdEfConverter
{
    public static AuthSessionId FromDatabase(string raw)
    {
        if (!AuthSessionId.TryCreate(raw, out var id))
        {
            throw new InvalidOperationException(
                $"Invalid AuthSessionId value in database: '{raw}'");
        }

        return id;
    }

    public static string ToDatabase(AuthSessionId id)
        => id.Value;

    public static AuthSessionId? FromDatabaseNullable(string? raw)
    {
        if (raw is null)
            return null;

        if (!AuthSessionId.TryCreate(raw, out var id))
        {
            throw new InvalidOperationException(
                $"Invalid AuthSessionId value in database: '{raw}'");
        }

        return id;
    }

    public static string? ToDatabaseNullable(AuthSessionId? id) => id?.Value;
}
