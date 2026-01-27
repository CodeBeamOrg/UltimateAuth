using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    public sealed class DefaultCredentialEndpointHandler : ICredentialEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IAccessContextFactory _accessContextFactory;
        private readonly IUserCredentialsService _credentials;

        public DefaultCredentialEndpointHandler(
            IAuthFlowContextAccessor authFlow,
            IAccessContextFactory accessContextFactory,
            IUserCredentialsService credentials)
        {
            _authFlow = authFlow;
            _accessContextFactory = accessContextFactory;
            _credentials = credentials;
        }

        private bool TryGetAuthenticatedUser(out AuthFlowContext flow, out IResult? error)
        {
            flow = _authFlow.Current;

            if (!flow.IsAuthenticated || flow.UserKey is null)
            {
                error = Results.Unauthorized();
                return false;
            }

            error = null;
            return true;
        }

        public async Task<IResult> GetAllAsync(HttpContext ctx)
        {
            if (!TryGetAuthenticatedUser(out var flow, out var error))
                return error!;

            var accessContext = await _accessContextFactory.CreateAsync(
                flow,
                action: UAuthActions.Credentials.List,
                resource: "credentials",
                resourceId: flow.UserKey.Value);

            var result = await _credentials.GetAllAsync(
                accessContext,
                ctx.RequestAborted);

            return Results.Ok(result);
        }

        public async Task<IResult> AddAsync(HttpContext ctx)
        {
            if (!TryGetAuthenticatedUser(out var flow, out var error))
                return error!;

            var request = await ctx.ReadJsonAsync<AddCredentialRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                flow,
                action: UAuthActions.Credentials.Add,
                resource: "credentials",
                resourceId: flow.UserKey.Value);

            var result = await _credentials.AddAsync(
                accessContext,
                request,
                ctx.RequestAborted);

            return Results.Ok(result);
        }

        public async Task<IResult> ChangeAsync(string type, HttpContext ctx)
        {
            if (!TryGetAuthenticatedUser(out var flow, out var error))
                return error!;

            if (!CredentialTypeParser.TryParse(type, out var credentialType))
                return Results.BadRequest($"Unsupported credential type: {type}");

            var request = await ctx.ReadJsonAsync<ChangeCredentialRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                flow,
                action: UAuthActions.Credentials.Change,
                resource: "credentials",
                resourceId: flow.UserKey.Value);

            var result = await _credentials.ChangeAsync(
                accessContext,
                credentialType,
                request,
                ctx.RequestAborted);

            return Results.Ok(result);
        }

        public async Task<IResult> RevokeAsync(string type, HttpContext ctx)
        {
            if (!TryGetAuthenticatedUser(out var flow, out var error))
                return error!;

            if (!CredentialTypeParser.TryParse(type, out var credentialType))
                return Results.BadRequest($"Unsupported credential type: {type}");

            var request = await ctx.ReadJsonAsync<RevokeCredentialRequest>(ctx.RequestAborted);

            var accessContext = await _accessContextFactory.CreateAsync(
                flow,
                action: UAuthActions.Credentials.Revoke,
                resource: "credentials",
                resourceId: flow.UserKey.Value);

            await _credentials.RevokeAsync(accessContext, credentialType, request, ctx.RequestAborted);
            return Results.NoContent();
        }

        public async Task<IResult> ActivateAsync(string type, HttpContext ctx)
        {
            if (!TryGetAuthenticatedUser(out var flow, out var error))
                return error!;

            if (!CredentialTypeParser.TryParse(type, out var credentialType))
                return Results.BadRequest($"Unsupported credential type: {type}");

            var accessContext = await _accessContextFactory.CreateAsync(
                flow,
                action: UAuthActions.Credentials.Activate,
                resource: "credentials",
                resourceId: flow.UserKey.Value);

            await _credentials.ActivateAsync(accessContext, credentialType, ctx.RequestAborted);
            return Results.NoContent();
        }

        public async Task<IResult> DeleteAsync(string type, HttpContext ctx)
        {
            if (!TryGetAuthenticatedUser(out var flow, out var error))
                return error!;

            if (!CredentialTypeParser.TryParse(type, out var credentialType))
                return Results.BadRequest($"Unsupported credential type: {type}");

            var accessContext = await _accessContextFactory.CreateAsync(
                flow,
                action: UAuthActions.Credentials.Delete,
                resource: "credentials",
                resourceId: flow.UserKey.Value);

            await _credentials.DeleteAsync(accessContext, credentialType, ctx.RequestAborted);
            return Results.NoContent();
        }
    }

}
