using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class NullableSessionChainIdConverter : ValueConverter<SessionChainId?, Guid?>
{
    public NullableSessionChainIdConverter()
        : base(
            id => SessionChainIdEfConverter.ToDatabaseNullable(id),
            raw => SessionChainIdEfConverter.FromDatabaseNullable(raw))
    {
    }
}
