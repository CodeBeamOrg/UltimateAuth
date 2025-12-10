namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    public sealed class TenantResolutionContext
    {
        public string? Host { get; init; }
        public string? Path { get; init; }
        public IReadOnlyDictionary<string, string>? Headers { get; init; }
        public IReadOnlyDictionary<string, string>? Query { get; init; }
        public object? RawContext { get; init; }
    }

}
