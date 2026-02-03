namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthHubServerOptions
{
    public string? ClientBaseAddress { get; set; }

    public HashSet<string> AllowedClientOrigins { get; set; } = new();

    /// <summary>
    /// Lifetime of hub flow artifacts (UI orchestration).
    /// Should be short-lived.
    /// </summary>
    public TimeSpan FlowLifetime { get; set; } = TimeSpan.FromMinutes(2);

    public string? LoginPath { get; set; } = "/login";

    internal UAuthHubServerOptions Clone() => new()
    {
        ClientBaseAddress = ClientBaseAddress,
        AllowedClientOrigins = new HashSet<string>(AllowedClientOrigins),
        FlowLifetime = FlowLifetime,
        LoginPath = LoginPath
    };

}
