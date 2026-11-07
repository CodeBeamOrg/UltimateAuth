using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UserProfileValidator : IUserProfileValidator
{
    public Task<UserProfileValidationResult> ValidateAsync(AccessContext context, UserProfileInfo profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var errors = UserProfileSafetyValidation.Validate(profile);

        return Task.FromResult(errors.Count == 0
            ? UserProfileValidationResult.Success()
            : UserProfileValidationResult.Failed(errors));
    }
}
