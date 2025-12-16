namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Represents a set of authentication tokens issued as a result of a successful login.
    /// This model is intentionally extensible to support additional token types in the future.
    /// </summary>
    public sealed record AuthTokens
    {
        /// <summary>
        /// The issued access token.
        /// Always present when <see cref="AuthTokens"/> is returned.
        /// </summary>
        public AccessToken AccessToken { get; init; } = default!;

        public RefreshToken? RefreshToken { get; init; }
    }
}
