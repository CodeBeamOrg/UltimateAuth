using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordValidationPipeline
{
    Task<PasswordPolicyResult> ValidateAsync(PasswordValidationRequest request, EffectivePasswordPolicyOptions options, CancellationToken ct = default);
}
