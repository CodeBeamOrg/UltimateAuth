namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record AddCredentialResult
{
    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public CredentialType? Type { get; init; }

    public static AddCredentialResult Success(CredentialType type)
        => new()
        {
            Succeeded = true,
            Type = type
        };

    public static AddCredentialResult Fail(string error)
        => new()
        {
            Succeeded = false,
            Error = error
        };
}
