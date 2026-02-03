using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Orchestrates the login flow.
/// Responsible for executing the login process by coordinating
/// credential validation, user resolution, authority decision,
/// and session creation.
/// </summary>
public interface ILoginOrchestrator<TUserId>
{
    Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default);
}
