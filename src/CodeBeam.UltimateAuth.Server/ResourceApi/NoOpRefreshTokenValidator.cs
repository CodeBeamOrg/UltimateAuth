using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class NoOpRefreshTokenValidator : IRefreshTokenValidator
{
    public Task ValidateAsync(RefreshTokenValidationContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    Task<RefreshTokenValidationResult> IRefreshTokenValidator.ValidateAsync(RefreshTokenValidationContext context, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
