using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal static class UAuthResultMapper
    {
        public static UAuthResult<T> FromJson<T>(UAuthTransportResult raw)
        {
            if (!raw.Ok)
            {
                return new UAuthResult<T>
                {
                    Ok = false,
                    Status = raw.Status
                };
            }

            if (raw.Body is null)
            {
                return new UAuthResult<T>
                {
                    Ok = true,
                    Status = raw.Status,
                    Value = default
                };
            }

            var value = raw.Body.Value.Deserialize<T>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return new UAuthResult<T>
            {
                Ok = true,
                Status = raw.Status,
                Value = value
            };
        }

        public static UAuthResult FromStatus(UAuthTransportResult raw)
            => new()
            {
                Ok = raw.Ok,
                Status = raw.Status
            };
    }
}
