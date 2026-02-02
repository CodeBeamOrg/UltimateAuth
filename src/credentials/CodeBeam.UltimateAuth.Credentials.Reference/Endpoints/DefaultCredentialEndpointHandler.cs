using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class DefaultCredentialEndpointHandler : ICredentialEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAccessContextFactory _accessContextFactory;
    private readonly IUserCredentialsService _credentials;

    public DefaultCredentialEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IUserCredentialsService credentials)
    {
        _authFlow = authFlow;
        _accessContextFactory = accessContextFactory;
        _credentials = credentials;
    }

    public async Task<IResult> GetAllAsync(HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.ListSelf,
            resource: "credentials",
            resourceId: flow.UserKey!.Value);

        var result = await _credentials.GetAllAsync(accessContext, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> AddAsync(HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        var request = await ctx.ReadJsonAsync<AddCredentialRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.AddSelf,
            resource: "credentials",
            resourceId: flow.UserKey!.Value);

        var result = await _credentials.AddAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> ChangeAsync(string type, HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        if (!TryParseType(type, out var credentialType, out error))
            return error!;

        var request = await ctx.ReadJsonAsync<ChangeCredentialRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.ChangeSelf,
            resource: "credentials",
            resourceId: flow.UserKey!.Value);

        var result = await _credentials.ChangeAsync(
            accessContext, credentialType, request, ctx.RequestAborted);

        return Results.Ok(result);
    }

    public async Task<IResult> RevokeAsync(string type, HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        if (!TryParseType(type, out var credentialType, out error))
            return error!;

        var request = await ctx.ReadJsonAsync<RevokeCredentialRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.RevokeSelf,
            resource: "credentials",
            resourceId: flow.UserKey!.Value);

        await _credentials.RevokeAsync(accessContext, credentialType, request, ctx.RequestAborted);
        return Results.NoContent();
    }

    public async Task<IResult> BeginResetAsync(string type, HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        if (!TryParseType(type, out var credentialType, out error))
            return error!;

        var request = await ctx.ReadJsonAsync<BeginCredentialResetRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.BeginResetSelf,
            resource: "credentials",
            resourceId: flow.UserKey!.Value);

        await _credentials.BeginResetAsync(accessContext, credentialType, request, ctx.RequestAborted);
        return Results.NoContent();
    }

    public async Task<IResult> CompleteResetAsync(string type, HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        if (!TryParseType(type, out var credentialType, out error))
            return error!;

        var request = await ctx.ReadJsonAsync<CompleteCredentialResetRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.CompleteResetSelf,
            resource: "credentials",
            resourceId: flow.UserKey!.Value);

        await _credentials.CompleteResetAsync(accessContext, credentialType, request, ctx.RequestAborted);
        return Results.NoContent();
    }

    public async Task<IResult> GetAllAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.ListAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        var result = await _credentials.GetAllAsync(accessContext, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> AddAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<AddCredentialRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.AddAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        var result = await _credentials.AddAsync(accessContext, request, ctx.RequestAborted);

        return Results.Ok(result);
    }

    public async Task<IResult> RevokeAdminAsync(UserKey userKey, string type, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        if (!TryParseType(type, out var credentialType, out var error))
            return error!;

        var request = await ctx.ReadJsonAsync<RevokeCredentialRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.RevokeAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        await _credentials.RevokeAsync(accessContext, credentialType, request, ctx.RequestAborted);

        return Results.NoContent();
    }

    public async Task<IResult> ActivateAdminAsync(UserKey userKey, string type, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        if (!TryParseType(type, out var credentialType, out var error))
            return error!;

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.ActivateAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        await _credentials.ActivateAsync(accessContext, credentialType, ctx.RequestAborted);

        return Results.NoContent();
    }

    public async Task<IResult> DeleteAdminAsync(UserKey userKey, string type, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        if (!TryParseType(type, out var credentialType, out var error))
            return error!;

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.DeleteAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        await _credentials.DeleteAsync(accessContext, credentialType, ctx.RequestAborted);

        return Results.NoContent();
    }

    public async Task<IResult> BeginResetAdminAsync(UserKey userKey, string type, HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        if (!TryParseType(type, out var credentialType, out error))
            return error!;

        var request = await ctx.ReadJsonAsync<BeginCredentialResetRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.BeginResetAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        await _credentials.BeginResetAsync(accessContext, credentialType, request, ctx.RequestAborted);
        return Results.NoContent();
    }

    public async Task<IResult> CompleteResetAdminAsync(UserKey userKey, string type, HttpContext ctx)
    {
        if (!TryGetSelf(out var flow, out var error))
            return error!;

        if (!TryParseType(type, out var credentialType, out error))
            return error!;

        var request = await ctx.ReadJsonAsync<CompleteCredentialResetRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Credentials.CompleteResetAdmin,
            resource: "credentials",
            resourceId: userKey.Value);

        await _credentials.CompleteResetAsync(accessContext, credentialType, request, ctx.RequestAborted);
        return Results.NoContent();
    }

    private bool TryGetSelf(out AuthFlowContext flow, out IResult? error)
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

    private static bool TryParseType(string type, out CredentialType credentialType, out IResult? error)
    {
        if (!CredentialTypeParser.TryParse(type, out credentialType))
        {
            error = Results.BadRequest($"Unsupported credential type: {type}");
            return false;
        }

        error = null;
        return true;
    }
}
