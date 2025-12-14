namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record LoginRequest
    {
        public string? TenantId { get; init; }
        public string Identifier { get; init; } = default!; // username, email etc.
        public string Secret { get; init; } = default!;     // password
        public string? DeviceId { get; init; }
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    }
}
