using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthIdentifierBehaviorOptions
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

    public int MaxIdentifiersPerUser { get; set; } = 10;
    public Dictionary<UserIdentifierType, int> MaxIdentifiersPerType { get; set; } = new();

    internal UAuthIdentifierBehaviorOptions Clone() => new()
    {
        AllowUsernameChange = AllowUsernameChange,
        AllowMultipleUsernames = AllowMultipleUsernames,
        AllowMultipleEmail = AllowMultipleEmail,
        AllowMultiplePhone = AllowMultiplePhone,
        RequireUsernameIdentifier = RequireUsernameIdentifier,
        RequireEmailVerification = RequireEmailVerification,
        RequirePhoneVerification = RequirePhoneVerification,
        AllowAdminOverride = AllowAdminOverride,
        AllowUserOverride = AllowUserOverride,

        MaxIdentifiersPerUser = MaxIdentifiersPerUser,
        MaxIdentifiersPerType = new Dictionary<UserIdentifierType, int>(MaxIdentifiersPerType)
    };
}
