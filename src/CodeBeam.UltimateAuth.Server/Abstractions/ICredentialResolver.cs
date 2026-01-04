using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    /// <summary>
    /// Gets the credential from the HTTP context.
    /// IPrimaryCredentialResolver is used to determine which kind of credential to resolve.
    /// </summary>
    public interface ICredentialResolver
    {
        ResolvedCredential? Resolve(HttpContext context, EffectiveAuthResponse response);
    }
}
