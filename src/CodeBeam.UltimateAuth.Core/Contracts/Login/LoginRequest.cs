using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record LoginRequest
    {
        public string? TenantId { get; init; }
        public string Identifier { get; init; } = default!; // username, email etc.
        public string Secret { get; init; } = default!;     // password
        public DateTimeOffset? At { get; init; }
        public DeviceInfo DeviceInfo { get; init; }
        public IReadOnlyDictionary<string, string>? Metadata { get; init; }

        /// <summary>
        /// Hint to request access/refresh tokens when the server mode supports it.
        /// Server policy may still ignore this.
        /// </summary>
        public bool RequestTokens { get; init; }

        // Optional
        public ChainId? ChainId { get; init; }
    }
}
