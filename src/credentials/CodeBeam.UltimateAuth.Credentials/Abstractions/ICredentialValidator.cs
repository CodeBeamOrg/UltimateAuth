using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialValidator
{
    Task<CredentialValidationResult> ValidateAsync<TUserId>(ICredential<TUserId> credential, string providedSecret, CancellationToken ct = default);
}
