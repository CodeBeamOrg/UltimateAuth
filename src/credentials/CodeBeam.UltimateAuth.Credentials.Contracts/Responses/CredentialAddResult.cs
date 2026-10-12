using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialAddResult
{
    public bool IsSuccess { get; init; }

    public string? Error { get; init; }

    public Guid? Id { get; set; }
    public CredentialType? Type { get; init; }

    public static CredentialAddResult Success(Guid id, CredentialType type)
        => new()
        {
            IsSuccess = true,
            Id = id,
            Type = type,
            Error = null
        };

    public static CredentialAddResult Fail(string error)
        => new()
        {
            IsSuccess = false,
            Error = error,
            Id = null,
            Type = null
        };
}
