namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerEndpointOptions
{
    /// <summary>
    /// Base API route. Default: "/auth"
    /// Changing this prevents conflicts with other auth systems.
    /// </summary>
    public string BasePath { get; set; } = "/auth";

    public bool Authentication { get; set; } = true;
    public bool Pkce { get; set; } = true;
    //public bool Token { get; set; } = true;
    public bool Session { get; set; } = true;

    //public bool UserInfo { get; set; } = true;
    public bool UserLifecycle { get; set; } = true;
    public bool UserProfile { get; set; } = true;
    public bool UserIdentifier { get; set; } = true;
    public bool Credentials { get; set; } = true;

    public bool Authorization { get; set; } = true;

    public HashSet<string> DisabledActions { get; set; } = new();

    public bool IsDisabled(string action) => DisabledActions.Contains(action);

    internal UAuthServerEndpointOptions Clone() => new()
    {
        Authentication = Authentication,
        Pkce = Pkce,
        //Token = Token,
        Session = Session,
        //UserInfo = UserInfo,
        UserLifecycle = UserLifecycle,
        UserProfile = UserProfile,
        UserIdentifier = UserIdentifier,
        Credentials = Credentials,
        Authorization = Authorization,
        DisabledActions = new HashSet<string>(DisabledActions)
    };
}
