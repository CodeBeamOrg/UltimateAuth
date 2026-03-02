using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record AddCredentialResult
{
    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public Guid? Id { get; set; }
    public CredentialType? Type { get; init; }

    public static AddCredentialResult Success(Guid id, CredentialType type)
        => new()
        {
            Succeeded = true,
            Id = id,
            Type = type,
            Error = null
        };

    public static AddCredentialResult Fail(string error)
        => new()
        {
            Succeeded = false,
            Error = error,
            Id = null,
            Type = null
        };
}
