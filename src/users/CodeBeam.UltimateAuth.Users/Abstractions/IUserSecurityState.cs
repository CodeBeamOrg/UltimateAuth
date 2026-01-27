namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityState
{
    long SecurityVersion { get; }
    bool IsLocked { get; }
    bool RequiresReauthentication { get; }
}
