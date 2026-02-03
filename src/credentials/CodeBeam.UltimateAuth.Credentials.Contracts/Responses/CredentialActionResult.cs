namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialActionResult
{
    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public static CredentialActionResult Success()
        => new()
        {
            Succeeded = true
        };

    public static CredentialActionResult Fail(string error)
        => new()
        {
            Succeeded = false,
            Error = error
        };
}
