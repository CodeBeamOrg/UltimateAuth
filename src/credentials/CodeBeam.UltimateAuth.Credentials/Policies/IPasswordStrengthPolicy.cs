using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public interface IPasswordStrengthPolicy
{
    Task<PasswordPolicyResult> ValidateAsync(PasswordPolicyContext context, EffectivePasswordPolicyOptions options, CancellationToken ct = default);
}
