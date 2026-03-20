using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class AuthSessionIdConverter : ValueConverter<AuthSessionId, string>
{
    public AuthSessionIdConverter() : base(id => AuthSessionIdEfConverter.ToDatabase(id), raw => AuthSessionIdEfConverter.FromDatabase(raw))
    {
    }
}

public sealed class NullableAuthSessionIdConverter : ValueConverter<AuthSessionId?, string?>
{
    public NullableAuthSessionIdConverter() : base(id => AuthSessionIdEfConverter.ToDatabaseNullable(id), raw => AuthSessionIdEfConverter.FromDatabaseNullable(raw))
    {
    }
}
