using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class RefreshEndpointHandler : IRefreshEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IRefreshFlowService _refreshFlow;
    private readonly ICredentialResponseWriter _credentialWriter;
    private readonly IRefreshResponseWriter _refreshWriter;
    private readonly IRefreshTokenResolver _refreshTokenResolver;
    private readonly IRefreshResponsePolicy _refreshPolicy;

    public RefreshEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IRefreshFlowService refreshFlow,
        ICredentialResponseWriter credentialWriter,
        IRefreshResponseWriter refreshWriter,
        IRefreshTokenResolver refreshTokenResolver,
        IRefreshResponsePolicy refreshPolicy)
    {
        _authContext = authContext;
        _refreshFlow = refreshFlow;
        _credentialWriter = credentialWriter;
        _refreshWriter = refreshWriter;
        _refreshTokenResolver = refreshTokenResolver;
        _refreshPolicy = refreshPolicy;
    }

    public async Task<IResult> RefreshAsync(HttpContext ctx)
    {
        var flow = _authContext.Current;

        if (flow == null)
        {
            return Results.BadRequest("No AuthFlowContext is found.");
        }

        var request = new RefreshFlowRequest
        {
            SessionId = flow.Session?.SessionId,
            RefreshToken = _refreshTokenResolver.Resolve(ctx),
            Device = flow.Device,
        };

        var result = await _refreshFlow.RefreshAsync(flow, request, ctx.RequestAborted);

        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var primary = _refreshPolicy.SelectPrimary(flow, request, result);

        if (primary == GrantKind.Session && result.SessionId is not null)
        {
            _credentialWriter.Write(ctx, GrantKind.Session, result.SessionId.Value);
        }
        else if (primary == GrantKind.AccessToken && result.AccessToken is not null)
        {
            _credentialWriter.Write(ctx, GrantKind.AccessToken, result.AccessToken);
        }

        if (_refreshPolicy.WriteRefreshToken(flow) && result.RefreshToken is not null)
        {
            _credentialWriter.Write(ctx, GrantKind.RefreshToken, result.RefreshToken);
        }

        if (flow.OriginalOptions.Diagnostics.EnableRefreshDetails)
        {
            _refreshWriter.Write(ctx, result.Outcome);
        }
        return Results.NoContent();
    }
}
