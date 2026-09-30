using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UserProfileValidator : IUserProfileValidator
{
    public Task<UserProfileValidationResult> ValidateAsync(AccessContext context, UserProfileInfo profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(UserProfileValidationResult.Success());
    }
}
