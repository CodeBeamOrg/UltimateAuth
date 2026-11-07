using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public interface IPasswordCommonPasswordChecker
{
    Task<bool> IsCommonAsync(string password, PasswordPolicyLevel level, CancellationToken ct = default);
}
