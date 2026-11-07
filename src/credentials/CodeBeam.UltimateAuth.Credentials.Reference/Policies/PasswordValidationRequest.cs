using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Policies;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed record PasswordValidationRequest
{
    public required PasswordPolicyContext Context { get; init; }

    // Null for first-time credential creation.
    public CredentialKey? ExistingCredentialKey { get; init; }
}
