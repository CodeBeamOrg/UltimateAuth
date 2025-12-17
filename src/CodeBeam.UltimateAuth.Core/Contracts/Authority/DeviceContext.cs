using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record DeviceContext
    {
        public string DeviceId { get; init; } = default!;

        public bool IsKnownDevice { get; init; }

        public bool IsTrusted { get; init; }

        public string? Platform { get; init; }

        public string? UserAgent { get; init; }

        public static DeviceContext From(DeviceInfo info)
        {
            return new DeviceContext
            {
                DeviceId = info.DeviceId,
                Platform = info.Platform,
                UserAgent = info.UserAgent
            };
        }
    }

}
