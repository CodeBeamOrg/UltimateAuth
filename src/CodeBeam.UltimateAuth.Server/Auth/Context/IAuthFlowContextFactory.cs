using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAuthFlowContextFactory
{
    ValueTask<AuthFlowContext> CreateAsync(HttpContext httpContext, AuthFlowType flowType, CancellationToken ct = default);
    ValueTask<AuthFlowContext> RecreateWithClientProfileAsync(AuthFlowContext existing, UAuthClientProfile overriddenProfile, CancellationToken ct = default);
}
