using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record SetInitialCredentialRequest
{
    /// <summary>
    /// Credential type to initialize (Password, Passkey, External, etc.).
    /// </summary>
    public required CredentialType Type { get; init; }

    /// <summary>
    /// Plain secret (password, passkey public data, external token reference).
    /// Will be hashed / processed by the credential service.
    /// </summary>
    public required string Secret { get; init; }
}
