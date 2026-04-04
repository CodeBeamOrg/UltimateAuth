using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

/// <summary>
/// Gets the credential from the HTTP context.
/// IPrimaryCredentialResolver is used to determine which kind of credential to resolve.
/// </summary>
public interface IValidateCredentialResolver
{
    Task<ResolvedCredential?> ResolveAsync(HttpContext context, EffectiveAuthResponse response);
}
