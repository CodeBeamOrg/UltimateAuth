using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    internal sealed class DefaultValidateEndpointHandler<TUserId> : IValidateEndpointHandler
    {
        private readonly ICredentialResolver _credentialResolver;
        private readonly ISessionQueryService<TUserId> _sessionValidator;
        private readonly IClock _clock;

        public DefaultValidateEndpointHandler(
            ICredentialResolver credentialResolver,
            ISessionQueryService<TUserId> sessionValidator,
            IClock clock)
        {
            _credentialResolver = credentialResolver;
            _sessionValidator = sessionValidator;
            _clock = clock;
        }

        public async Task<IResult> ValidateAsync(
            HttpContext context,
            CancellationToken ct = default)
        {
            var credential = _credentialResolver.Resolve(context);

            if (credential is null)
            {
                return Results.Json(
                    new ValidateResponse
                    {
                        Valid = false,
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
                        new ValidateResponse
                        {
                            Valid = false,
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

                if (!result.IsValid)
                {
                    return Results.Json(
                        new ValidateResponse
                        {
                            Valid = false,
                            State = result.State
                                .ToString()
                                .ToLowerInvariant()
                        },
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                return Results.Ok(new ValidateResponse { Valid = true });
            }

            // Stateless (JWT / Opaque) – 0.0.1 no support yet
            return Results.Json(
                new ValidateResponse
                {
                    Valid = false,
                    State = "unsupported"
                },
                statusCode: StatusCodes.Status401Unauthorized
            );
        }
    }
}
