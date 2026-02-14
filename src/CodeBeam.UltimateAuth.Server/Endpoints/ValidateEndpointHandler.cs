using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class ValidateEndpointHandler : IValidateEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IFlowCredentialResolver _credentialResolver;
    private readonly ISessionValidator _sessionValidator;
    private readonly IClock _clock;

    public ValidateEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IFlowCredentialResolver credentialResolver,
        ISessionValidator sessionValidator,
        IClock clock)
    {
        _authContext = authContext;
        _credentialResolver = credentialResolver;
        _sessionValidator = sessionValidator;
        _clock = clock;
    }

    public async Task<IResult> ValidateAsync(HttpContext context, CancellationToken ct = default)
    {
        var auth = _authContext.Current;
        var credential = _credentialResolver.Resolve(context, auth.Response);

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

            var tenant = context.GetTenant();

            var result = await _sessionValidator.ValidateSessionAsync(
                new SessionValidationContext
                {
                    Tenant = tenant,
                    SessionId = sessionId,
                    Now = _clock.UtcNow,
                    Device = auth.Device
                },
                ct);

            if (result.UserKey is not UserKey userKey)
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

            return Results.Ok(new AuthValidationResult
            {
                IsValid = result.IsValid,
                State = result.IsValid ? "active" : result.State.ToString().ToLowerInvariant(),
                Snapshot = new AuthStateSnapshot
                {
                    Identity = new AuthIdentity
                    {
                        UserKey = userKey,
                        Tenant = result.Tenant,
                        AuthenticatedAt = result.AuthenticatedAt
                    },
                    Claims = result.Claims
                }
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
