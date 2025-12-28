using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class NullableAuthSessionIdConverter : ValueConverter<AuthSessionId?, string?>
    {
        public NullableAuthSessionIdConverter()
            : base(
                v => v == null ? null : v.Value,
                v => v == null ? null : AuthSessionId.From(v))
        {
        }
    }
}
