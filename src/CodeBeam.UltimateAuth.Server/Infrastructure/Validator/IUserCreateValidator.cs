using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUserCreateValidator
{
    Task<UserCreateValidatorResult> ValidateAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default);
}
