namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class DeviceInfo
    {
        public string? Platform { get; init; }
        public string? OperatingSystem { get; init; }
        public string? Browser { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? Fingerprint { get; init; }
        public bool? IsTrusted { get; init; }

        public Dictionary<string, object>? Custom { get; init; }
    }
}
