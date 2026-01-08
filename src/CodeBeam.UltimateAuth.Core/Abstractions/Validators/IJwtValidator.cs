using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Validates access tokens (JWT or opaque) and resolves
    /// the authenticated user context.
    /// </summary>
    public interface IJwtValidator
    {
        Task<TokenValidationResult<TUserId>> ValidateAsync<TUserId>(string token, CancellationToken ct = default);
    }
}
