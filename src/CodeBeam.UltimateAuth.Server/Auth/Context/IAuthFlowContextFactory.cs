using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAuthFlowContextFactory
{
    ValueTask<AuthFlowContext> CreateAsync(HttpContext httpContext, AuthFlowType flowType, CancellationToken ct = default);
}
