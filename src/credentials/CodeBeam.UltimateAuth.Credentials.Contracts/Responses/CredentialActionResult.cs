namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialActionResult
{
    public bool IsSuccess { get; init; }

    public string? Error { get; init; }

    public static CredentialActionResult Success()
        => new()
        {
            IsSuccess = true
        };

    public static CredentialActionResult Fail(string error)
        => new()
        {
            IsSuccess = false,
            Error = error
        };
}
