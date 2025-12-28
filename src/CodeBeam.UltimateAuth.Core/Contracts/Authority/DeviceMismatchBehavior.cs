namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public enum DeviceMismatchBehavior
    {
        Reject,        // 401
        Allow,         // Accept session
        AllowAndRebind // Accept and update device info
    }

}
