namespace CodeBeam.UltimateAuth.Server.Credentials;

public interface ICredentialValidator
{
    Task<CredentialValidationResult> ValidateAsync<TUserId>(ICredential<TUserId> credential, string providedSecret, CancellationToken ct = default);
}
