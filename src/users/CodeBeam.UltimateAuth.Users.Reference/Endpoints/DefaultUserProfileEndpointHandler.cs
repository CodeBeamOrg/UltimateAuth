using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public sealed class DefaultUserProfileEndpointHandler : IUserProfileEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IAccessContextFactory _accessContextFactory;
        private readonly IUAuthUserProfileService _profiles;

        public DefaultUserProfileEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IUAuthUserProfileService profiles)
        {
            _authFlow = authFlow;
            _accessContextFactory = accessContextFactory;
            _profiles = profiles;
        }

        public async Task<IResult> GetAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated || flow.UserKey is null)
                return Results.Unauthorized();

            var accessContext = await _accessContextFactory.CreateAsync(
                authFlow: flow,
                action: UAuthActions.UserProfiles.GetSelf,
                resource: "users",
                resourceId: flow.UserKey.Value
            );

            var result = await _profiles.GetCurrentAsync(accessContext, ctx.RequestAborted);
            return Results.Ok(result);
        }

        public async Task<IResult> UpdateAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated || flow.UserKey is null)
                return Results.Unauthorized();

            var request = await ctx.ReadJsonAsync<UpdateProfileRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                authFlow: flow,
                action: UAuthActions.UserProfiles.UpdateSelf,
                resource: "users",
                resourceId: flow.UserKey.Value
            );

            await _profiles.UpdateCurrentAsync(accessContext, request, ctx.RequestAborted);
            return Results.NoContent();
        }

    }
}
