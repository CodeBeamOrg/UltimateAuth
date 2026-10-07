using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUserIdentifierValidator
{
    Task<UserIdentifierValidationResult> ValidateAsync(AccessContext context, UserIdentifierInfo identifier, CancellationToken ct = default);
}
