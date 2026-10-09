namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierVerificationResult
{
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }

    public static IdentifierVerificationResult Success() => new() { IsSuccess = true };

    public static IdentifierVerificationResult Failed(string reason) => new() { IsSuccess = false, FailureReason = reason };
}
