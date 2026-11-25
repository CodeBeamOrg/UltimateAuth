using Microsoft.AspNetCore.Http;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUAuthCookiePolicyBuilder
{
    CookieOptions Build(EffectiveGrantDelivery delivery, AuthFlowContext context, GrantKind kind);
}
