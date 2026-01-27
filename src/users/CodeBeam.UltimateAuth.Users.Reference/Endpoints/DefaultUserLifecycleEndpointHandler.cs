using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public sealed class DefaultUserLifecycleEndpointHandler : IUserLifecycleEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IAccessContextFactory _accessContextFactory;
        private readonly IUserLifecycleService _lifecycle;

        public DefaultUserLifecycleEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IUserLifecycleService lifecycle)
        {
            _authFlow = authFlow;
            _accessContextFactory = accessContextFactory;
            _lifecycle = lifecycle;
        }

        public async Task<IResult> CreateAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var request = await ctx.ReadJsonAsync<CreateUserRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                authFlow: flow,
                action: UAuthActions.Users.Create,
                resource: "users"
            );

            var result = await _lifecycle.CreateAsync(accessContext, request, ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

        public async Task<IResult> ChangeStatusAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var request = await ctx.ReadJsonAsync<ChangeUserStatusRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                authFlow: flow,
                action: UAuthActions.Users.ChangeStatus,
                resource: "users",
                resourceId: request.UserKey.Value
            );

            var result = await _lifecycle.ChangeStatusAsync(accessContext, request, ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

        public async Task<IResult> DeleteAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var request = await ctx.ReadJsonAsync<DeleteUserRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                authFlow: flow,
                action: UAuthActions.Users.Delete,
                resource: "users",
                resourceId: request.UserKey.Value,
                attributes: new Dictionary<string, object>
                {
                    ["deleteMode"] = request.Mode
                }
            );

            var result = await _lifecycle.DeleteAsync(accessContext, request, ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

    }
}
