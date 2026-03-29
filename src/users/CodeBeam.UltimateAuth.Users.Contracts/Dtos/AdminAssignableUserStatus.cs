namespace CodeBeam.UltimateAuth.Users.Contracts;

public enum AdminAssignableUserStatus
{
    Active = 0,

    Disabled = 20,
    Suspended = 30,

    Locked = 40,
    RiskHold = 50,

    PendingActivation = 60,
    PendingVerification = 70,

    Unknown = 100
}
