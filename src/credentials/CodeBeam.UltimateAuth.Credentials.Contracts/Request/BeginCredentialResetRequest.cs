using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record BeginCredentialResetRequest
{
    public string Identifier { get; init; } = default!;
    public CredentialType CredentialType { get; set; } = CredentialType.Password;
    public ResetCodeType ResetCodeType { get; set; }
    public string? Channel { get; init; }
    public TimeSpan? Validity { get; init; }
}
