using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record BeginResetCredentialRequest
{
    public required string Identifier { get; init; }
    public CredentialType CredentialType { get; init; }
    public ResetCodeType ResetCodeType { get; init; }
    public string? Channel { get; init; }
    public TimeSpan? Validity { get; init; }
}
