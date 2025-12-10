using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Core.Utilities
{
    public sealed class UAuthUserIdConverter<TUserId> : IUserIdConverter<TUserId>
    {
        public string ToString(TUserId id)
        {
            return id switch
            {
                int v => v.ToString(CultureInfo.InvariantCulture),
                long v => v.ToString(CultureInfo.InvariantCulture),
                Guid v => v.ToString("N"),
                string v => v,
                _ => JsonSerializer.Serialize(id)
            };
        }

        public byte[] ToBytes(TUserId id) => Encoding.UTF8.GetBytes(ToString(id));

        public TUserId FromString(string value)
        {
            return typeof(TUserId) switch
            {
                Type t when t == typeof(int) => (TUserId)(object)int.Parse(value),
                Type t when t == typeof(long) => (TUserId)(object)long.Parse(value),
                Type t when t == typeof(Guid) => (TUserId)(object)Guid.Parse(value),
                Type t when t == typeof(string) => (TUserId)(object)value,
                _ => JsonSerializer.Deserialize<TUserId>(value)
                    ?? throw new UAuthInternalException("Cannot deserialize TUserId")
            };
        }

        public TUserId FromBytes(byte[] binary) => FromString(Encoding.UTF8.GetString(binary));
    }
}
