using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options;

public interface IEffectiveServerOptionsProvider
{
    UAuthServerOptions GetOriginal(HttpContext context);
    EffectiveUAuthServerOptions GetEffective(HttpContext context, UAuthClientProfile clientProfile);
    EffectiveUAuthServerOptions GetEffective(TenantKey tenant, UAuthClientProfile clientProfile);
}
