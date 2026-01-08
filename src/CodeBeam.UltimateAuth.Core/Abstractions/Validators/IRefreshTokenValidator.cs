using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IRefreshTokenValidator<TUserId>
{
    Task<RefreshTokenValidationResult<TUserId>> ValidateAsync(
        string? tenantId,
        string refreshToken,
        DateTimeOffset now,
        CancellationToken ct = default);
}
