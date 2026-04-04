namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthHubOptions
{
    public string? ClientBaseAddress { get; set; }

    public HashSet<string> AllowedClientOrigins { get; set; } = new();

    /// <summary>
    /// Lifetime of hub flow artifacts (UI orchestration).
    /// Should be short-lived.
    /// </summary>
    public TimeSpan FlowLifetime { get; set; } = TimeSpan.FromMinutes(5);

    public string? LoginPath { get; set; } = "/login";

    internal UAuthHubOptions Clone() => new()
    {
        ClientBaseAddress = ClientBaseAddress,
        AllowedClientOrigins = new HashSet<string>(AllowedClientOrigins),
        FlowLifetime = FlowLifetime,
        LoginPath = LoginPath
    };
}
