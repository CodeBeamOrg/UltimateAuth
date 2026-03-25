using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Authorization;

public sealed class UAuthAuthorizationHandler : AuthorizationHandler<UAuthActionRequirement>
{
    private readonly IAccessOrchestrator _orchestrator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UAuthAuthorizationHandler(
        IAccessOrchestrator orchestrator,
        IHttpContextAccessor httpContextAccessor)
    {
        _orchestrator = orchestrator;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UAuthActionRequirement requirement)
    {
        var http = _httpContextAccessor.HttpContext!;
        var accessContext = ResourceAccessContextBuilder.Create(http, requirement.Action);

        try
        {
            await _orchestrator.ExecuteAsync(
                accessContext,
                new AccessCommand(_ => Task.CompletedTask));

            context.Succeed(requirement);
        }
        catch (UAuthAuthorizationException)
        {
            // deny
        }
    }
}
