using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Abstractions;

public interface IPureJwtLogoutService
{
    Task<bool> LogoutAsync(AuthFlowContext flow, string refreshToken, CancellationToken ct = default);
}
