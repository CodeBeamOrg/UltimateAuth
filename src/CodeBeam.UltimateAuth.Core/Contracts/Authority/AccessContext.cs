using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class AccessContext
    {
        public UserKey? UserKey { get; init; }
        public string Action { get; init; } = default!;
        public string? Resource { get; init; }
        public string? ResourceId { get; init; }
        public IReadOnlyDictionary<string, object>? Attributes { get; init; }
    }
}
