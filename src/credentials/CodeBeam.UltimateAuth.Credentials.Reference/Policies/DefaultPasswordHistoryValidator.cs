using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class DefaultPasswordHistoryValidator(IPasswordCredentialStoreFactory credentialStoreFactory, IPasswordHistoryStoreFactory historyStoreFactory, IUAuthPasswordHasher hasher) : IPasswordHistoryValidator
{
    public async Task<PasswordPolicyResult> ValidateAsync(CredentialKey credentialKey, string password, int rememberLast, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (rememberLast is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(rememberLast));

        var credentialStore = credentialStoreFactory.Create(credentialKey.Tenant);

        var credential = await credentialStore.GetAsync(credentialKey, ct);

        if (credential is null)
            throw new InvalidOperationException("Password credential was not found.");

        if (hasher.Verify(credential.SecretHash, password))
            return Reused();

        if (rememberLast == 0)
            return PasswordPolicyResult.Success();

        var historyStore = historyStoreFactory.Create(credentialKey.Tenant);
        var previous = await historyStore.GetRecentAsync(credentialKey, rememberLast, ct);

        foreach (var entry in previous)
        {
            ct.ThrowIfCancellationRequested();

            if (hasher.Verify(entry.SecretHash, password))
                return Reused();
        }

        return PasswordPolicyResult.Success();
    }

    private static PasswordPolicyResult Reused()
        => PasswordPolicyResult.Failure(new PasswordPolicyViolation(PasswordPolicyErrorCodes.ReusedPassword, "Password was used previously."));
}
