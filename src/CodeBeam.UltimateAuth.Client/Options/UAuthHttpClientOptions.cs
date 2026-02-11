using CodeBeam.UltimateAuth.Client.Defaults;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthHttpClientOptions
{
    public bool AutoRegister { get; set; } = true;
    public string Name { get; set; } = UAuthClientDefaults.HttpClientName;
}
