namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum UserStatus
{

    Active = 0,

    SelfSuspended = 10,

    Disabled = 20,
    Suspended = 30,

    Locked = 40,
    RiskHold = 50,

    PendingActivation = 60,
    PendingVerification = 70,

    Unknown = 99
}
