using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    internal sealed class DefaultValidateEndpointHandler<TUserId> : IValidateEndpointHandler
    {
        private readonly ICredentialResolver _credentialResolver;
        private readonly ISessionQueryService<TUserId> _sessionValidator;
        private readonly IAuthFlowContextFactory _flowFactory;
        private readonly IAuthResponseResolver _responseResolver;
        private readonly IClock _clock;

        public DefaultValidateEndpointHandler(
            ICredentialResolver credentialResolver,
            ISessionQueryService<TUserId> sessionValidator,
            IAuthFlowContextFactory flowFactory,
            IAuthResponseResolver responseResolver,
            IClock clock)
        {
            _credentialResolver = credentialResolver;
            _sessionValidator = sessionValidator;
            _flowFactory = flowFactory;
            _responseResolver = responseResolver;
            _clock = clock;
        }

        public async Task<IResult> ValidateAsync(
            HttpContext context,
            CancellationToken ct = default)
        {
            var flow = _flowFactory.Create(context, AuthFlowType.Validate);
            var response = _responseResolver.Resolve(flow);
            var credential = _credentialResolver.Resolve(context, response);

            if (credential is null)
            {
                return Results.Json(
                    new AuthValidationResult
                    {
                        IsValid = false,
                        State = "missing"
                    },
                    statusCode: StatusCodes.Status401Unauthorized
                );
            }

            if (credential.Kind == PrimaryCredentialKind.Stateful)
            {
                if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
                {
                    return Results.Json(
                        new AuthValidationResult
                        {
                            IsValid = false,
                            State = "invalid"
                        },
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                var result = await _sessionValidator.ValidateSessionAsync(
                    new SessionValidationContext
                    {
                        TenantId = credential.TenantId,
                        SessionId = sessionId,
                        Now = _clock.UtcNow,
                        Device = credential.Device
                    },
                    ct);

                return Results.Ok(new AuthValidationResult
                {
                    IsValid = result.IsValid,
                    State = result.IsValid ? "active" : result.State.ToString().ToLowerInvariant()
                });
            }

            // Stateless (JWT / Opaque) – 0.0.1 no support yet
            return Results.Json(
                new AuthValidationResult
                {
                    IsValid = false,
                    State = "unsupported"
                },
                statusCode: StatusCodes.Status401Unauthorized
            );
        }
    }
}
