using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IIdentifierValidator
{
    Task<IdentifierValidationResult> ValidateAsync(AccessContext context, UserIdentifierInfo identifier, CancellationToken ct = default);
}
