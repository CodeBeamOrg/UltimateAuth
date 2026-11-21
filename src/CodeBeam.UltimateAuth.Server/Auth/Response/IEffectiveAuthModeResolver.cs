using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

/// <summary>
/// Resolves the authentication mode for a given execution context.
/// </summary>
/// <remarks>
/// Implementations may select modes using tenant configuration, trusted endpoint metadata or authenticated client information.
/// <br/><br/>
/// The selected mode must be permitted by the server configuration.
/// Client-supplied profile information alone must not be used to grant access to a more privileged authentication path.
/// <br/><br/>
/// Register a custom implementation through dependency injection to replace the default mode selection policy.
/// </remarks>
public interface IEffectiveAuthModeResolver
{
    /// <summary>
    /// Selects the authentication mode for the supplied context.
    /// </summary>
    /// <param name="context">
    /// Context containing the tenant, application profile and optional HTTP request information.
    /// </param>
    /// <returns>The selected authentication mode.</returns>
    UAuthMode Resolve(AuthModeResolutionContext context);
}
