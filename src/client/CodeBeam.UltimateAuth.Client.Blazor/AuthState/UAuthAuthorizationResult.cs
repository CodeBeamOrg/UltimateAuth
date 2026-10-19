namespace CodeBeam.UltimateAuth.Client.Blazor;

public enum UAuthAuthorizationStatus
{
    Authorized = 0,
    Denied = 10,
    Inactive = 20
}

public readonly record struct UAuthAuthorizationResult(UAuthAuthorizationStatus Status)
{
    public bool IsAuthorized => Status == UAuthAuthorizationStatus.Authorized;

    public bool IsInactive => Status == UAuthAuthorizationStatus.Inactive;

    public static UAuthAuthorizationResult Authorized => new(UAuthAuthorizationStatus.Authorized);

    public static UAuthAuthorizationResult Denied => new(UAuthAuthorizationStatus.Denied);

    public static UAuthAuthorizationResult Inactive => new(UAuthAuthorizationStatus.Inactive);
}
