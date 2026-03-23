namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UserAgentInfo
{
    public string? DeviceType { get; init; }
    public string? Platform { get; init; }
    public string? OperatingSystem { get; init; }
    public string? Browser { get; init; }
}
