using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Runtime;

public sealed class UAuthClientProductInfo
{
    public string ProductName { get; init; } = "UltimateAuth Client";
    public string Version { get; init; } = default!;
    public string? InformationalVersion { get; init; }

    public UAuthClientProfile ClientProfile { get; init; } = default!;

    public DateTimeOffset StartedAt { get; init; }
    public string RuntimeId { get; init; } = Guid.NewGuid().ToString("n");

    public bool AutoRefreshEnabled { get; init; }
    public TimeSpan? RefreshInterval { get; init; }
    public ReauthBehavior ReauthBehavior { get; init; }

    public string FrameworkDescription { get; init; } = default!;
}
