using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IRefreshTokenValidator<TUserId>
{
    Task<RefreshTokenValidationResult<TUserId>> ValidateAsync(RefreshTokenValidationContext<TUserId> context, CancellationToken ct = default);
}
