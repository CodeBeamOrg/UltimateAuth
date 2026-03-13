namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class DeviceContext
{
    public DeviceId? DeviceId { get; init; }
    public string? DeviceType { get; init; }
    public string? OperatingSystem { get; init; }
    public string? Platform { get; init; }
    public string? Browser { get; init; }
    public string? IpAddress { get; init; }

    public bool HasDeviceId => DeviceId is not null;

    private DeviceContext(
        DeviceId? deviceId,
        string? deviceType,
        string? platform,
        string? operatingSystem,
        string? browser,
        string? ipAddress)
    {
        DeviceId = deviceId;
        DeviceType = deviceType;
        Platform = platform;
        OperatingSystem = operatingSystem;
        Browser = browser;
        IpAddress = ipAddress;
    }

    public static DeviceContext Anonymous()
        => new(
            deviceId: null,
            deviceType: null,
            platform: null,
            operatingSystem: null,
            browser: null,
            ipAddress: null);

    public static DeviceContext Create(
        DeviceId deviceId,
        string? deviceType,
        string? platform,
        string? operatingSystem,
        string? browser,
        string? ipAddress)
    {
        return new DeviceContext(
            deviceId,
            Normalize(deviceType),
            Normalize(platform),
            Normalize(operatingSystem),
            Normalize(browser),
            Normalize(ipAddress));
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();

    // DeviceInfo is a transport object.
    // AuthFlowContextFactory changes it to a useable DeviceContext
    // DeviceContext doesn't have fields like IsTrusted etc. It's authority layer's responsibility.
    // Geo and Fingerprint will be added here.

}
