using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUserProfileValidator
{
    Task<UserProfileValidationResult> ValidateAsync(AccessContext context, UserProfileInfo profile, CancellationToken ct = default);
}
