using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class DefaultPasswordValidationPipeline(IPasswordStrengthPolicy strengthPolicy, IPasswordHistoryValidator historyValidator) : IPasswordValidationPipeline
{
    public async Task<PasswordPolicyResult> ValidateAsync(PasswordValidationRequest request, EffectivePasswordPolicyOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);

        ct.ThrowIfCancellationRequested();

        var password = request.Context.Password;

        if (password is null)
        {
            return PasswordPolicyResult.Failure(new PasswordPolicyViolation(PasswordPolicyErrorCodes.Required, "Password is required."));
        }

        // Mandatory guard, independent of custom strength policies.
        if (password.Length > PasswordPolicyLimits.AbsoluteMaxLength)
        {
            return PasswordPolicyResult.Failure(new PasswordPolicyViolation(PasswordPolicyErrorCodes.InputTooLong, "Password exceeds the absolute length limit."));
        }

        var strength = await strengthPolicy.ValidateAsync(request.Context, options, ct);
        var violations = new List<PasswordPolicyViolation>(strength.Violations);

        if (request.ExistingCredentialKey is { } credentialKey)
        {
            var history = await historyValidator.ValidateAsync(credentialKey, password, options.RememberLast, ct);
            violations.AddRange(history.Violations);
        }

        return new PasswordPolicyResult(violations);
    }
}
