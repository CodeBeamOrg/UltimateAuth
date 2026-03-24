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
    private readonly IValidateCredentialResolver _credentialResolver;
    private readonly ISessionValidator _sessionValidator;
    private readonly IAuthStateSnapshotFactory _snapshotFactory;
    private readonly IClock _clock;

    public ValidateEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IValidateCredentialResolver credentialResolver,
        ISessionValidator sessionValidator,
        IAuthStateSnapshotFactory snapshotFactory,
        IClock clock)
    {
        _authContext = authContext;
        _credentialResolver = credentialResolver;
        _sessionValidator = sessionValidator;
        _snapshotFactory = snapshotFactory;
        _clock = clock;
    }

    public async Task<IResult> ValidateAsync(HttpContext context, CancellationToken ct = default)
    {
        var auth = _authContext.Current;
        var credential = await _credentialResolver.ResolveAsync(context, auth.Response);

        if (credential is null)
        {
            return Results.Json(
                new AuthValidationResult
                {
                    State = SessionState.NotFound
                },
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        if (credential.Kind == PrimaryGrantKind.Stateful)
        {
            if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            {
                return Results.Json(
                    new AuthValidationResult
                    {
                        State = SessionState.Invalid
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
                        State = SessionState.Invalid
                    },
                    statusCode: StatusCodes.Status401Unauthorized
                );
            }

            var snapshot = await _snapshotFactory.CreateAsync(result);

            return Results.Ok(new AuthValidationResult
            {
                State = result.IsValid ? SessionState.Active : result.State,
                Snapshot = snapshot
            });
        }

        // Stateless (JWT / Opaque) – 0.0.1 no support yet
        return Results.Json(
            new AuthValidationResult
            {
                State = SessionState.Unsupported
            },
            statusCode: StatusCodes.Status401Unauthorized
        );
    }
}
