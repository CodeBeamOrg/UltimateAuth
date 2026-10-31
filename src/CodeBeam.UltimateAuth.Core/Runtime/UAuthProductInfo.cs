namespace CodeBeam.UltimateAuth.Core.Runtime;

public sealed class UAuthProductInfo
{
    public string ProductName { get; init; } = "UltimateAuth";
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
}
