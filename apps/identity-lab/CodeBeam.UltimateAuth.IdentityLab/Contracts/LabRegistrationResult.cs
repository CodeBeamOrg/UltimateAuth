namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record LabRegistrationResult(bool IsSuccess, string? ErrorMessage = null, bool RequiresRepair = false)
{
    public static LabRegistrationResult Success() => new(true);

    public static LabRegistrationResult Failed(string message, bool requiresRepair = false)
        => new(false, message, requiresRepair);
}
