using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    public sealed class DefaultAuthorizationEndpointHandler : IAuthorizationEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IAuthorizationService _authorization;
        private readonly IUserRoleService _roles;
        private readonly IAccessContextFactory _accessContextFactory;

        public DefaultAuthorizationEndpointHandler(IAuthFlowContextAccessor authFlow, IAuthorizationService authorization, IUserRoleService roles, IAccessContextFactory accessContextFactory)
        {
            _authFlow = authFlow;
            _authorization = authorization;
            _roles = roles;
            _accessContextFactory = accessContextFactory;
        }

        public async Task<IResult> CheckAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;
            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<AuthorizationCheckRequest>(ctx.RequestAborted);

            var accessContext = _accessContextFactory.Create(
                flow,
                action: req.Action,
                resource: req.Resource,
                resourceId: req.ResourceId
            );

            var result = await _authorization.AuthorizeAsync(accessContext, ctx.RequestAborted);

            if (result.RequiresReauthentication)
                return Results.StatusCode(StatusCodes.Status428PreconditionRequired);

            return result.IsAllowed
                ? Results.Ok(result)
                : Results.Forbid();
        }

        public async Task<IResult> GetRolesAsync(UserKey userKey, HttpContext ctx)
        {
            var flow = _authFlow.Current;
            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var accessContext = _accessContextFactory.Create(
                flow,
                action: UAuthActions.Authorization.Roles.Read,
                resource: "authorization.roles",
                resourceId: userKey.Value
            );

            var roles = await _roles.GetRolesAsync(accessContext, userKey, ctx.RequestAborted);

            return Results.Ok(new UserRolesResponse
            {
                UserKey = userKey,
                Roles = roles
            });
        }

        public async Task<IResult> AssignRoleAsync(UserKey userKey, HttpContext ctx)
        {
            var flow = _authFlow.Current;
            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<AssignRoleRequest>(ctx.RequestAborted);

            var accessContext = _accessContextFactory.Create(
                flow,
                action: UAuthActions.Authorization.Roles.Assign,
                resource: "authorization.roles",
                resourceId: userKey.Value
            );

            await _roles.AssignAsync(accessContext, userKey, req.Role, ctx.RequestAborted);
            return Results.Ok();
        }

        public async Task<IResult> RemoveRoleAsync(UserKey userKey, HttpContext ctx)
        {
            var flow = _authFlow.Current;
            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<AssignRoleRequest>(ctx.RequestAborted);

            var accessContext = _accessContextFactory.Create(
                flow,
                action: UAuthActions.Authorization.Roles.Remove,
                resource: "authorization.roles",
                resourceId: userKey.Value
            );

            await _roles.RemoveAsync(accessContext, userKey, req.Role, ctx.RequestAborted);
            return Results.Ok();
        }
    }
}
