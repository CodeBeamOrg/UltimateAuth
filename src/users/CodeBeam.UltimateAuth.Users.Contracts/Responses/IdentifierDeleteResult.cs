namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierDeleteResult
{
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }

    public static IdentifierDeleteResult Success() => new() { IsSuccess = true };
    public static IdentifierDeleteResult Fail(string reason) => new() { IsSuccess = false, FailureReason = reason };
}
