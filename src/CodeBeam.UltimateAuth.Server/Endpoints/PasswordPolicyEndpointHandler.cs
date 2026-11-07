
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class PasswordPolicyEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IAccessOrchestrator accessOrchestrator) : IPasswordPolicyEndpointHandler
{
    public async Task<IResult> GetAsync(HttpContext context)
    {
        var flow = authFlow.Current;

        var accessContext = await accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.GetPasswordPolicyAnonymous,
            resource: "credentials",
            ct: context.RequestAborted);

        var command = new AccessCommand<UAuthPasswordPolicyInfo>(
            ct =>
            {
                ct.ThrowIfCancellationRequested();

                var effective = PasswordPolicyOptionsResolver.Resolve(flow.Options.Password);

                return Task.FromResult(effective.ToInfo());
            });

        var info = await accessOrchestrator.ExecuteAsync(accessContext, command, context.RequestAborted);

        context.Response.Headers.CacheControl = "no-store";

        return Results.Ok(info);
    }
}
