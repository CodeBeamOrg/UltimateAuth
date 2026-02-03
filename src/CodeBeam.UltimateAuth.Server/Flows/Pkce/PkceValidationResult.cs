namespace CodeBeam.UltimateAuth.Server.Flows;

public sealed class PkceValidationResult
{
    private PkceValidationResult(bool success, PkceValidationFailureReason reason)
    {
        Success = success;
        FailureReason = reason;
    }

    public bool Success { get; }

    public PkceValidationFailureReason FailureReason { get; }

    public static PkceValidationResult Ok() => new(true, PkceValidationFailureReason.None);

    public static PkceValidationResult Fail(PkceValidationFailureReason reason) => new(false, reason);
}
