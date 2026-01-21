namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialResult(
    bool Succeeded,
    string? Error,
    CredentialType? Type = null)
{
    public static ChangeCredentialResult Success(CredentialType type)
        => new(true, null, type);

    public static ChangeCredentialResult Fail(string error)
        => new(false, error);
}
