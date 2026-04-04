using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class SessionChainIdConverter : ValueConverter<SessionChainId, Guid>
{
    public SessionChainIdConverter()
        : base(
            id => SessionChainIdEfConverter.ToDatabase(id),
            raw => SessionChainIdEfConverter.FromDatabase(raw))
    {
    }
}