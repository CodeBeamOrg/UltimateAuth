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
        var info = new UserIdentifierInfo
        {
            Type = request.Type,
            Value = request.Value
        };

        var validation = await _validator.ValidateAsync(context, info, ct);

        if (!validation.IsValid)
        {
            return new UserIdentifierAvailabilityResult
            {
                IsValid = false,
                IsAvailable = false,
                Errors = validation.Errors
            };
        }

        var normalized = _normalizer.Normalize(request.Type, request.Value).Normalized;

        var store = _storeFactory.Create(context.ResourceTenant);

        var existence = await store.ExistsAsync(new IdentifierExistenceQuery(request.Type, normalized, IdentifierExistenceScope.TenantAny), ct);

        return new UserIdentifierAvailabilityResult
        {
            IsValid = true,
            IsAvailable = !existence.Exists,
            NormalizedValue = normalized
        };
    }
}
