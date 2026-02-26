using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public sealed class CredentialValidator : ICredentialValidator
{
    private readonly IUAuthPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public CredentialValidator(IUAuthPasswordHasher passwordHasher, IClock clock)
    {
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public Task<CredentialValidationResult> ValidateAsync(ICredential credential, string providedSecret, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (credential is ICredentialDescriptor securable)
        {
            if (!securable.Security.IsUsable(_clock.UtcNow))
            {
                return Task.FromResult(CredentialValidationResult.Failed(reason: "credential_not_usable"));
            }
        }

        if (credential is ISecretCredential secret)
        {
            var ok = _passwordHasher.Verify(secret.SecretHash, providedSecret);

            return Task.FromResult(ok
                ? CredentialValidationResult.Success()
                : CredentialValidationResult.Failed(reason: "invalid_credentials"));
        }

        return Task.FromResult(CredentialValidationResult.Failed(reason: "unsupported_credential_type"));
    }
}
