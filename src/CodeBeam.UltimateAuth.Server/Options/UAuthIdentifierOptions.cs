namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthIdentifierOptions
{
    public bool AllowUsernameChange { get; set; } = true;
    public bool AllowMultipleUsernames { get; set; } = false;
    public bool AllowMultipleEmail { get; set; } = true;
    public bool AllowMultiplePhone { get; set; } = true;

    public bool RequireUsernameIdentifier { get; set; } = false;
    public bool RequireEmailVerification { get; set; } = false;
    public bool RequirePhoneVerification { get; set; } = false;

    public bool AllowAdminOverride { get; set; } = true;
    public bool AllowUserOverride { get; set; } = true;

    internal UAuthIdentifierOptions Clone() => new()
    {
        AllowUsernameChange = AllowUsernameChange,
        AllowMultipleUsernames = AllowMultipleUsernames,
        AllowMultipleEmail = AllowMultipleEmail,
        AllowMultiplePhone = AllowMultiplePhone,
        RequireUsernameIdentifier = RequireUsernameIdentifier,
        RequireEmailVerification = RequireEmailVerification,
        RequirePhoneVerification = RequirePhoneVerification,
        AllowAdminOverride = AllowAdminOverride,
        AllowUserOverride = AllowUserOverride
    };
}
