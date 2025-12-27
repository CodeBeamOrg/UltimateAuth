using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure.Internal;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultRefreshEndpointHandler<TUserId> : IRefreshEndpointHandler where TUserId : notnull
    {
        private readonly UAuthServerOptions _options;
        private readonly ISessionContextAccessor _sessionContextAccessor;
        private readonly ISessionQueryService<TUserId> _sessionQueries;
        private readonly ISessionRefreshService<TUserId> _sessionRefresh;
        private readonly ICredentialResponseWriter _credentialResponseWriter;
        private readonly IRefreshResponseWriter _refreshResponseWriter;

        public DefaultRefreshEndpointHandler(
            IOptions<UAuthServerOptions> options,
            ISessionContextAccessor sessionContextAccessor,
            ISessionQueryService<TUserId> sessionQueries,
            ISessionRefreshService<TUserId> sessionRefresh,
            ICredentialResponseWriter credentialResponseWriter,
            IRefreshResponseWriter refreshResponseWriter)
        {
            _options = options.Value;
            _sessionContextAccessor = sessionContextAccessor;
            _sessionQueries = sessionQueries;
            _sessionRefresh = sessionRefresh;
            _credentialResponseWriter = credentialResponseWriter;
            _refreshResponseWriter = refreshResponseWriter;
        }

        public async Task<IResult> RefreshAsync(HttpContext ctx)
        {
            var decision = RefreshDecisionResolver.Resolve(_options);

            if (decision != RefreshDecision.SessionOnly)
            {
                // Endpoint exists, but this mode does not support session refresh
                return Results.StatusCode(StatusCodes.Status409Conflict);
            }

            var sessionContext = _sessionContextAccessor.Current;
            if (sessionContext?.SessionId is null)
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            var validation = await _sessionQueries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = sessionContext.TenantId,
                    SessionId = (AuthSessionId)sessionContext.SessionId,
                    Now = now,
                    Device = DeviceInfoFactory.FromHttpContext(ctx)
                },
                ctx.RequestAborted);


            if (!validation.IsValid)
                return Results.Unauthorized();

            var refreshResult = await _sessionRefresh.RefreshAsync(validation, now, ctx.RequestAborted);

            if (!refreshResult.IsSuccess || refreshResult.PrimaryToken is null)
                return Results.Unauthorized();

            _credentialResponseWriter.Write(ctx, refreshResult.PrimaryToken.Value,
                new CredentialResponseOptions
                {
                    Mode = TokenResponseMode.Cookie
                });

            _refreshResponseWriter.Write(ctx, RefreshOutcome.NoOp);

            return Results.NoContent();
        }
    }
}
