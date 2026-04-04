using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAuthFlow
{
    ValueTask<AuthFlowContext> BeginAsync(AuthFlowType flowType, CancellationToken ct = default);
}
