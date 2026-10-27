namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public enum OwnerRepairState
{
    NotRequired,
    CanRepair,
    NotEligible
}

public sealed record OwnerRepairStatus(OwnerRepairState State, string Message);

public sealed record OwnerRepairResult(bool Success, string Message)
{
    public static OwnerRepairResult Ok(string message)
        => new(true, message);

    public static OwnerRepairResult Fail(string message)
        => new(false, message);
}
