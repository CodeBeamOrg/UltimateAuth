using Microsoft.AspNetCore.Http;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUAuthCookiePolicyBuilder
{
    CookieOptions Build(CredentialResponseOptions response, AuthFlowContext context, GrantKind kind);
}
