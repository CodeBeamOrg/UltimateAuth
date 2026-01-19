using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public sealed class DefaultUserEndpointHandler : IUserEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IUserLifecycleService _lifecycle;

        public DefaultUserEndpointHandler(IAuthFlowContextAccessor authFlow, IUserLifecycleService lifecycle)
        {
            _authFlow = authFlow;
            _lifecycle = lifecycle;
        }

        public async Task<IResult> CreateAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<CreateUserRequest>(ctx.RequestAborted);
            var result = await _lifecycle.CreateAsync(flow.TenantId, req, ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

        public async Task<IResult> ChangeStatusAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<ChangeUserStatusRequest>(ctx.RequestAborted);
            var result = await _lifecycle.ChangeStatusAsync(flow.TenantId, req, ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

        public async Task<IResult> DeleteAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<DeleteUserRequest>(ctx.RequestAborted);
            var result = await _lifecycle.DeleteAsync(flow.TenantId, req,ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

    }
}
