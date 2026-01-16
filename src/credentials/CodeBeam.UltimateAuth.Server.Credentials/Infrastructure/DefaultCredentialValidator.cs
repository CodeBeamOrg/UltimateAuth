using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Server.Credentials;

public sealed class DefaultCredentialValidator : ICredentialValidator
{
    private readonly IUAuthPasswordHasher _passwordHasher;

    public DefaultCredentialValidator(IUAuthPasswordHasher passwordHasher) => _passwordHasher = passwordHasher;

    public Task<CredentialValidationResult> ValidateAsync<TUserId>(ICredential<TUserId> credential, string providedSecret, CancellationToken ct = default)
    {
        if (credential is not ISecretCredential<TUserId> secret)
        {
            return Task.FromResult(new CredentialValidationResult(
                IsValid: false,
                RequiresReauthentication: false,
                RequiresSecurityVersionIncrement: false,
                FailureReason: "Unsupported credential type."));
        }

        var ok = _passwordHasher.Verify(secret.SecretHash, providedSecret);

        return Task.FromResult(ok
            ? new CredentialValidationResult(true, false, false)
            : new CredentialValidationResult(false, false, false, "Invalid credentials."));
    }
}
