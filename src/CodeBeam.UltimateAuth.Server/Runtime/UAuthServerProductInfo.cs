using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Runtime;

public sealed class UAuthServerProductInfo
{
    public string ProductName { get; init; } = "UltimateAuth Server";
    public string Version { get; init; } = default!;
    public string? InformationalVersion { get; init; }

    public string DisplayVersion
    {
        get
        {
            var version = InformationalVersion?.Split('+', 2)[0];

            return string.IsNullOrWhiteSpace(version)
                ? Version
                : version;
        }
    }

    public DateTimeOffset StartedAt { get; init; }
    public string RuntimeId { get; init; } = Guid.NewGuid().ToString("n");

    public UAuthHubDeploymentMode HubDeploymentMode { get; init; }
    public bool MultiTenancyEnabled { get; init; }
    public string? FrameworkDescription { get; set; }
}
