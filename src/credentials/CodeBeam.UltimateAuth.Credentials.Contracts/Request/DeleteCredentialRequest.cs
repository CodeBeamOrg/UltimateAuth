using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public class DeleteCredentialRequest
{
    public Guid Id { get; init; }
    public DeleteMode Mode { get; set; } = DeleteMode.Soft;
}
