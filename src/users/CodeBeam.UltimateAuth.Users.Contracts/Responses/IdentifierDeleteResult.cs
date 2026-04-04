namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierDeleteResult
{
    public bool Succeeded { get; init; }
    public string? FailureReason { get; init; }

    public static IdentifierDeleteResult Success() => new() { Succeeded = true };
    public static IdentifierDeleteResult Fail(string reason) => new() { Succeeded = false, FailureReason = reason };
}
