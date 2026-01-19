using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    public sealed class DefaultCredentialEndpointHandler : ICredentialEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IUserCredentialsService _credentials;

        public DefaultCredentialEndpointHandler(
            IAuthFlowContextAccessor authFlow,
            IUserCredentialsService credentials)
        {
            _authFlow = authFlow;
            _credentials = credentials;
        }

        public async Task<IResult> SetInitialAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<SetInitialCredentialRequest>(ctx.RequestAborted);
            var result = await _credentials.SetInitialAsync(flow.TenantId, flow.UserKey!.Value, req, ctx.RequestAborted);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }

        public async Task<IResult> ResetPasswordAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<ResetPasswordRequest>(ctx.RequestAborted);
            await _credentials.ResetAsync(flow.TenantId, req.UserKey, req, ctx.RequestAborted);

            return Results.Ok();
        }

        public async Task<IResult> RevokeAllAsync(HttpContext ctx)
        {
            var flow = _authFlow.Current;

            if (!flow.IsAuthenticated)
                return Results.Unauthorized();

            var req = await ctx.ReadJsonAsync<RevokeAllCredentialsRequest>(ctx.RequestAborted);
            await _credentials.RevokeAllAsync(flow.TenantId, req, ctx.RequestAborted);

            return Results.Ok();
        }
    }
}
