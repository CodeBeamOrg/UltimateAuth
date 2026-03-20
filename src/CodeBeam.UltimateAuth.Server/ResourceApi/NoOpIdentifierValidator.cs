using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NoOpIdentifierValidator : IIdentifierValidator
{
    public Task<IdentifierValidationResult> ValidateAsync(AccessContext context, UserIdentifierInfo identifier, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
