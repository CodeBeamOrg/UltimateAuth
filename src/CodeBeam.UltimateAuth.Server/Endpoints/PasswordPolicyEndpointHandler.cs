using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class PasswordPolicyEndpointHandler(IAuthFlowContextAccessor authFlow) : IPasswordPolicyEndpointHandler
{
    public IResult GetAsync(HttpContext context)
    {
        var flow = authFlow.Current;
        var effective = PasswordPolicyOptionsResolver.Resolve(flow.Options.Password);

        UAuthPasswordPolicyInfo info = effective.ToInfo();

        context.Response.Headers.CacheControl = "no-store";

        return Results.Ok(info);
    }
}
