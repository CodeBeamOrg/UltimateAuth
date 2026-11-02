using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordHistoryValidator
{
    Task<PasswordPolicyResult> ValidateAsync(CredentialKey credentialKey, string password, int rememberLast, CancellationToken ct = default);
}
