using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IRefreshTokenValidator
{
    Task<RefreshTokenValidationResult> ValidateAsync(RefreshTokenValidationContext context, CancellationToken ct = default);
}
