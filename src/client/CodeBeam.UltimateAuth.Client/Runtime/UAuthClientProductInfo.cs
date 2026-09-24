using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Runtime;

/// <summary>
/// Represents product information for the UltimateAuth client, including versioning, client profile, and runtime details.
/// </summary>
public sealed class UAuthClientProductInfo
{
    /// <summary>
    /// Gets the name of the product. This is a read-only property initialized to "UltimateAuth Client".
    /// </summary>
    public string ProductName { get; init; } = "UltimateAuth Client";

    /// <summary>
    /// Gets the version of the product. This is a required property that must be initialized with a valid version string.
    /// </summary>
    public string Version { get; init; } = default!;

    /// <summary>
    /// Gets the informational version of the product. This is an optional property that can be initialized with a version string for informational purposes.
    /// </summary>
    public string? InformationalVersion { get; init; }


    /// <summary>
    /// Gets the client profile associated with the UltimateAuth client. This is a required property that must be initialized with a valid UAuthClientProfile value.
    /// </summary>
    public UAuthClientProfile ClientProfile { get; init; } = default!;


    /// <summary>
    /// Gets the timestamp indicating when the UltimateAuth client started. This is a required property that must be initialized with a valid DateTimeOffset value.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the runtime identifier for the UltimateAuth client. This is a read-only property initialized with a new GUID in string format, which uniquely identifies the runtime instance.
    /// </summary>
    public string RuntimeId { get; init; } = Guid.NewGuid().ToString("n");


    /// <summary>
    /// Gets a value indicating whether auto-refresh is enabled for the UltimateAuth client. This is a required property that must be initialized with a boolean value.
    /// </summary>
    public bool AutoRefreshEnabled { get; init; }

    /// <summary>
    /// Gets the refresh interval for the UltimateAuth client. This is an optional property that can be initialized with a TimeSpan value indicating how often the client should refresh its state. If not set, the client may use a default refresh interval.
    /// </summary>
    public TimeSpan? RefreshInterval { get; init; }

    /// <summary>
    /// Gets the reauthentication behavior for the UltimateAuth client. This is a required property that must be initialized with a valid ReauthBehavior value, which determines how the client handles reauthentication scenarios.
    /// </summary>
    public ReauthBehavior ReauthBehavior { get; init; }

    /// <summary>
    /// Gets the framework description for the UltimateAuth client. This is a required property that must be initialized with a string value describing the framework in which the client is running (e.g., ".NET 6.0", ".NET 7.0").
    /// </summary>
    public string FrameworkDescription { get; init; } = default!;
}
