using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Validation
{
    internal sealed class InMemoryPasswordCredentialValidator : ICredentialValidator
    {
        private readonly IUAuthPasswordHasher _hasher;

        public InMemoryPasswordCredentialValidator(IUAuthPasswordHasher hasher)
        {
            _hasher = hasher;
        }

        public Task<CredentialValidationResult> ValidateAsync<TUserId>(ICredential<TUserId> credential, string providedSecret, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (credential is not PasswordCredential<TUserId> pwd)
                return Task.FromResult(CredentialValidationResult.Failed());

            if (!pwd.IsActive)
                return Task.FromResult(CredentialValidationResult.Failed());

            var ok = _hasher.Verify(pwd.SecretHash, providedSecret);

            return Task.FromResult(ok
                ? CredentialValidationResult.Success()
                : CredentialValidationResult.Failed());
        }
    }
}
