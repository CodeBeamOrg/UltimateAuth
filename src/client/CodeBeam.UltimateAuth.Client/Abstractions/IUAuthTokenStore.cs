using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Abstractions;

/// <summary>
/// Stores the access credential associated with the current client scope.
/// </summary>
/// <remarks>
/// The store does not determine the authentication mode, application profile,
/// grant delivery mechanism, or transport.
/// </remarks>
public interface IUAuthTokenStore
{
    /// <summary>
    /// Returns the currently stored access token, if any.
    /// </summary>
    AccessToken? Read();

    /// <summary>
    /// Atomically replaces the current access token.
    /// </summary>
    void Replace(AccessToken token);

    /// <summary>
    /// Removes the current access token.
    /// </summary>
    void Clear();
}
