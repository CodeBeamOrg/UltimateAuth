namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record VerifyUserIdentifierRequest
{
    public Guid Id { get; init; }

    /// <summary>
    /// Required for self verification.
    /// Not required for authorized admin verification.
    /// </summary>
    public string? Proof { get; init; }
}
