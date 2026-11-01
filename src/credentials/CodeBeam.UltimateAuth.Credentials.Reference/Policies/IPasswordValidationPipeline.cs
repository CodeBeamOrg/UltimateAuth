using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordValidationPipeline
{
    Task<PasswordPolicyResult> ValidateAsync(PasswordValidationRequest request, CancellationToken ct = default);
}
