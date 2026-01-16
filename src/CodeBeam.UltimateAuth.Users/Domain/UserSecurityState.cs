namespace CodeBeam.UltimateAuth.Server.Users;

public sealed record UserSecurityState(
    long SecurityVersion,
    bool IsLocked,
    bool RequiresReauthentication)
    : IUserSecurityState;
