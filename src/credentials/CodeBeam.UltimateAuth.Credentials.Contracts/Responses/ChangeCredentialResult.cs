namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialResult
{
    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public CredentialType? Type { get; init; }

    public static ChangeCredentialResult Success(CredentialType type)
        => new()
        {
            Succeeded = true,
            Type = type
        };

    public static ChangeCredentialResult Fail(string error)
        => new()
        {
            Succeeded = false,
            Error = error
        };
}
