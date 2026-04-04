using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialResult
{
    public bool IsSuccess { get; init; }

    public string? Error { get; init; }

    public CredentialType? Type { get; init; }

    public static ChangeCredentialResult Success(CredentialType type)
        => new()
        {
            IsSuccess = true,
            Type = type
        };

    public static ChangeCredentialResult Fail(string error)
        => new()
        {
            IsSuccess = false,
            Error = error
        };
}
