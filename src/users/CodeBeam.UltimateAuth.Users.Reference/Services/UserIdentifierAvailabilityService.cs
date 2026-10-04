using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.Options;

public sealed class UserIdentifierAvailabilityService : IUserIdentifierAvailabilityService
{
    private readonly IUserIdentifierValidator _validator;
    private readonly IIdentifierNormalizer _normalizer;
    private readonly IUserIdentifierStoreFactory _storeFactory;
    private readonly UAuthServerOptions _options;

    public UserIdentifierAvailabilityService(IUserIdentifierValidator validator, IIdentifierNormalizer normalizer, IUserIdentifierStoreFactory storeFactory, IOptions<UAuthServerOptions> options)
    {
        _validator = validator;
        _normalizer = normalizer;
        _storeFactory = storeFactory;
        _options = options.Value;
    }

    public async Task<UserIdentifierAvailabilityResult> CheckAsync(AccessContext context, CheckUserIdentifierAvailabilityRequest request, CancellationToken ct = default)
    {
        var identifier = new UserIdentifierInfo
        {
            Type = request.Type,
            Value = request.Value
        };

        var validation = await _validator.ValidateAsync(context, identifier, ct);

        if (!validation.IsValid)
        {
            return UserIdentifierAvailabilityResult.Invalid(validation.Errors);
        }

        var normalized = _normalizer.Normalize(request.Type, request.Value);

        if (!normalized.IsValid)
        {
            return UserIdentifierAvailabilityResult.Invalid(
                new[]
                {
                    new UAuthValidationError(normalized.ErrorCode ?? "identifier_invalid")
                });
        }

        var uniquenessScope = IdentifierUniquenessResolver.GetScope(_options, request.Type);

        if (uniquenessScope is UniquenessScope.None or UniquenessScope.WithinUser)
        {
            if (context.TargetUserKey is null)
            {
                return UserIdentifierAvailabilityResult.Available(normalized.Normalized);
            }
        }

        var store = _storeFactory.Create(context.ResourceTenant);

        var query = uniquenessScope switch
        {
            UniquenessScope.Tenant =>
                new IdentifierExistenceQuery(
                    request.Type,
                    normalized.Normalized,
                    IdentifierExistenceScope.TenantAny),

            UniquenessScope.WithinUser =>
                new IdentifierExistenceQuery(
                    request.Type,
                    normalized.Normalized,
                    IdentifierExistenceScope.WithinUser,
                    context.TargetUserKey),

            UniquenessScope.None =>
                new IdentifierExistenceQuery(
                    request.Type,
                    normalized.Normalized,
                    IdentifierExistenceScope.WithinUser,
                    context.TargetUserKey),

            _ => throw new InvalidOperationException($"Unsupported uniqueness scope '{uniquenessScope}'.")
        };

        var existence = await store.ExistsAsync(query, ct);

        return existence.Exists
            ? UserIdentifierAvailabilityResult.Unavailable(normalized.Normalized)
            : UserIdentifierAvailabilityResult.Available(normalized.Normalized);
    }
}
