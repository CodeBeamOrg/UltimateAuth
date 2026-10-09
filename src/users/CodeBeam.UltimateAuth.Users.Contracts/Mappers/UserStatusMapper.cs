using CodeBeam.UltimateAuth.Core.Contracts;
using System.Net.NetworkInformation;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public static class UserStatusMapper
{
    public static UserStatus ToUserStatus(this SelfAssignableUserStatus selfStatus)
    {
        switch (selfStatus)
        {
            case SelfAssignableUserStatus.Active:
                return UserStatus.Active;
            case SelfAssignableUserStatus.SelfSuspended:
                return UserStatus.SelfSuspended;
            default:
                throw new ArgumentOutOfRangeException(nameof(selfStatus), selfStatus, "Unsupported user status.");
        }
    }

    public static UserStatus ToUserStatus(this AdminAssignableUserStatus status)
    {
        switch (status)
        {
            case AdminAssignableUserStatus.Active:
                return UserStatus.Active;
            case AdminAssignableUserStatus.Suspended:
                return UserStatus.Suspended;
            case AdminAssignableUserStatus.Disabled:
                return UserStatus.Disabled;
            case AdminAssignableUserStatus.Locked:
                return UserStatus.Locked;
            case AdminAssignableUserStatus.RiskHold:
                return UserStatus.RiskHold;
            case AdminAssignableUserStatus.PendingActivation:
                return UserStatus.PendingActivation;
            case AdminAssignableUserStatus.PendingVerification:
                return UserStatus.PendingVerification;
            case AdminAssignableUserStatus.Unknown:
                return UserStatus.Unknown;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported user status.");
        }
    }

    public static AdminAssignableUserStatus ToAdminAssignableUserStatus(this UserStatus status)
    {
        switch (status)
        {
            case UserStatus.Active:
                return AdminAssignableUserStatus.Active;
            case UserStatus.Suspended:
                return AdminAssignableUserStatus.Suspended;
            case UserStatus.Disabled:
                return AdminAssignableUserStatus.Disabled;
            case UserStatus.Locked:
                return AdminAssignableUserStatus.Locked;
            case UserStatus.RiskHold:
                return AdminAssignableUserStatus.RiskHold;
            case UserStatus.PendingActivation:
                return AdminAssignableUserStatus.PendingActivation;
            case UserStatus.PendingVerification:
                return AdminAssignableUserStatus.PendingVerification;
            case UserStatus.Unknown:
                return AdminAssignableUserStatus.Unknown;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported user status.");
        }
    }
}
