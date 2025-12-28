using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Runtime
{
    public sealed class UAuthProductInfo
    {
        public string ProductName { get; init; } = "UltimateAuth";
        public string Version { get; init; } = default!;
        public string? InformationalVersion { get; init; }

        public UAuthClientProfile ClientProfile { get; init; }
        public bool ClientProfileAutoDetected { get; init; }

        public DateTimeOffset StartedAt { get; init; }
        public string RuntimeId { get; init; } = Guid.NewGuid().ToString("n");
    }
}
