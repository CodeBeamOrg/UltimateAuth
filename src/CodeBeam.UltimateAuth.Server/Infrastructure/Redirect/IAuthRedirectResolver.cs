using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IAuthRedirectResolver
{
    RedirectDecision ResolveSuccess(AuthFlowContext flow, HttpContext context);
    RedirectDecision ResolveFailure(AuthFlowContext flow, HttpContext context, AuthFailureReason reason, LoginResult? loginResult = null);
}
