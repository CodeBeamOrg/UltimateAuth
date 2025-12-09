namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class SessionMetadata
    {
        public string? AppVersion { get; init; }
        public string? Locale { get; init; }
        public string? TenantId { get; init; }
        public string? CsrfToken { get; init; }

        public Dictionary<string, object>? Custom { get; init; }
    }
}
