namespace CodeBeam.UltimateAuth.Server.Options;

public class UAuthUserProfileOptions
{
    public bool EnableMultiProfile { get; set; } = false;

    internal UAuthUserProfileOptions Clone() => new()
    {
        EnableMultiProfile = EnableMultiProfile
    };
}
