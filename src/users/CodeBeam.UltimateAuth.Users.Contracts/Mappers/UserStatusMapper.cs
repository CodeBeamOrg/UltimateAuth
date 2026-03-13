using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public static class UserStatusMapper
{
    public static UserStatus ToUserStatus(this SelfUserStatus selfStatus)
    {
        switch (selfStatus)
        {
            case SelfUserStatus.Active:
                return UserStatus.Active;
            case SelfUserStatus.SelfSuspended:
                return UserStatus.SelfSuspended;
            default:
                throw new NotImplementedException();
        }
    }
}
