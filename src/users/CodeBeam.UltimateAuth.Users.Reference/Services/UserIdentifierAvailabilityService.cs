using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserIdentifierAvailabilityService : IUserIdentifierAvailabilityService
{
    private readonly IUserIdentifierValidator _validator;
    private readonly IIdentifierNormalizer _normalizer;
    private readonly IUserIdentifierStoreFactory _storeFactory;

    public UserIdentifierAvailabilityService(IUserIdentifierValidator validator, IIdentifierNormalizer normalizer, IUserIdentifierStoreFactory storeFactory)
    {
        _validator = validator;
        _normalizer = normalizer;
        _storeFactory = storeFactory;
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

        var store = _storeFactory.Create(context.ResourceTenant);

        var existence = await store.ExistsAsync(
            new IdentifierExistenceQuery(
                request.Type,
                normalized.Normalized,
                IdentifierExistenceScope.TenantAny),
            ct);

        return existence.Exists
            ? UserIdentifierAvailabilityResult.Unavailable(normalized.Normalized)
            : UserIdentifierAvailabilityResult.Available(normalized.Normalized);
    }
}
