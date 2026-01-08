using Microsoft.AspNetCore.Http;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Cookies;

public interface IUAuthCookiePolicyBuilder
{
    CookieOptions Build(CredentialResponseOptions response, AuthFlowContext context, TimeSpan? logicalLifetime);
}
