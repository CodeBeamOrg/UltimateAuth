using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    public sealed record ResolvedCredential
    {
        public PrimaryCredentialKind Kind { get; init; }

        /// <summary>
        /// Raw credential value (session id / jwt / opaque)
        /// </summary>
        public string Value { get; init; } = default!;

        public string? TenantId { get; init; } = default!;

        public DeviceInfo Device { get; init; } = default!;
    }
}
