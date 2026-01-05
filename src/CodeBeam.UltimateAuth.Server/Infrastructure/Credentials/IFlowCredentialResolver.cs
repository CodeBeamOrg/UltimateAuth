using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// Gets the credential from the HTTP context.
    /// IPrimaryCredentialResolver is used to determine which kind of credential to resolve.
    /// </summary>
    public interface IFlowCredentialResolver
    {
        ResolvedCredential? Resolve(HttpContext context, EffectiveAuthResponse response);
    }
}
