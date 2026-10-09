namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierChangeResult
{
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }

    public static IdentifierChangeResult Success() => new() { IsSuccess = true };

    public static IdentifierChangeResult Failed(string reason) => new() { IsSuccess = false, FailureReason = reason };
}
