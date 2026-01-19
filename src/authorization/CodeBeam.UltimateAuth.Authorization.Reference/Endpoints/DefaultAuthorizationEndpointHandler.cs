using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
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

        public DefaultAuthorizationEndpointHandler(IAuthFlowContextAccessor authFlow, IAuthorizationService authorization, IUserRoleService roles)
        {
            _authFlow = authFlow;
            _authorization = authorization;
            _roles = roles;
        }

        public async Task<IResult> CheckAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<AuthorizationCheckRequest>(ctx.RequestAborted);

            var result = await _authorization.AuthorizeAsync(flow.TenantId,
                new AccessContext
                {
                    UserKey = flow.UserKey!.Value,
                    Action = req.Action,
                    Resource = req.Resource,
                    ResourceId = req.ResourceId
                },
                ctx.RequestAborted);

            return result.IsAllowed
                ? Results.Ok(result)
                : Results.Forbid();
        }

        public async Task<IResult> GetRolesAsync(UserKey userKey, HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var roles = await _roles.GetRolesAsync(flow.TenantId, userKey, ctx.RequestAborted);

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
            await _roles.AssignAsync(flow.TenantId, userKey, req.Role, ctx.RequestAborted);

            return Results.Ok();
        }

        public async Task<IResult> RemoveRoleAsync(UserKey userKey, HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<AssignRoleRequest>(ctx.RequestAborted);
            await _roles.RemoveAsync(flow.TenantId, userKey, req.Role, ctx.RequestAborted);

            return Results.Ok();
        }
    }
}
