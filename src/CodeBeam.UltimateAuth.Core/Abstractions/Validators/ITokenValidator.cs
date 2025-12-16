using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Validates access tokens (JWT or opaque) and resolves
    /// the authenticated user context.
    /// </summary>
    public interface ITokenValidator
    {
        Task<TokenValidationResult<TUserId>> ValidateAsync<TUserId>(
            string token,
            TokenType type,
            CancellationToken ct = default);
    }
}
