namespace CodeBeam.UltimateAuth.Client.Abstractions;

/// <summary>
/// Represents a provider that can retrieve the current return URL, typically used in authentication flows to redirect users back to their original destination after login or other actions.
/// </summary>
public interface IReturnUrlProvider
{
    /// <summary>
    /// Gets the current return URL, which is the URL to which the user should be redirected after completing an authentication flow or other relevant action.
    /// </summary>
    string GetCurrentUrl();
}
