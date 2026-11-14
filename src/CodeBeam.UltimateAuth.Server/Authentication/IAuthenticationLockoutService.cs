using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Authentication;

public interface IAuthenticationLockoutService
{
    Task<AuthenticationLockoutInfo> GetAsync(AccessContext context, LockoutQueryRequest request, CancellationToken ct = default);
    Task<AuthenticationUnlockResult> UnlockAsync(AccessContext context, UnlockAuthenticationRequest request, CancellationToken ct = default);
}
