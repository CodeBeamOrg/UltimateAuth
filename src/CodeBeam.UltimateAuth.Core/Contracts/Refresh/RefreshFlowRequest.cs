using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class RefreshFlowRequest
    {
        public AuthSessionId? SessionId { get; init; }
        public string? RefreshToken { get; init; }
        public DeviceInfo Device { get; init; } = DeviceInfo.Unknown;
        public DateTimeOffset Now { get; init; }
    }
}
