using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class TransportCredential
    {
        public required TransportCredentialKind Kind { get; init; }
        public required string Value { get; init; }

        public string? TenantId { get; init; }
        public DeviceInfo? Device { get; init; }
    }
}
