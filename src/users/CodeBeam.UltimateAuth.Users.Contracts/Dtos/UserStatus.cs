namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public enum UserStatus
    {
        // Normal state
        Active = 0,

        // User initiated
        SelfSuspended = 10,

        // Administrative actions
        Disabled = 20,
        Suspended = 30,

        // Security / risk based
        Locked = 40,
        RiskHold = 50,

        // Lifecycle
        PendingActivation = 60,
        PendingVerification = 70,

        // Terminal (soft-delete)
        Deactivated = 80,

        // Soft // TODO: User domain already have IsDeleted, this may remove
        Deleted = 90
    }
}
