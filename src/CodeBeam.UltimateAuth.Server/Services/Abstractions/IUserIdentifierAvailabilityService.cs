using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Services;

public interface IUserIdentifierAvailabilityService
{
    Task<UserIdentifierAvailabilityResult> CheckAsync(AccessContext context, CheckUserIdentifierAvailabilityRequest request, CancellationToken ct = default);
}
