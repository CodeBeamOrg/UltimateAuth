using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;

namespace CodeBeam.UltimateAuth.Server.Services;

/// <summary>
/// Handles authentication flows such as login,
/// logout, session refresh and reauthentication.
/// </summary>
public interface IUAuthFlowService
{
    Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default);

    Task<LoginResult> LoginAsync(AuthFlowContext auth, AuthExecutionContext execution, LoginRequest request, CancellationToken ct);

    Task<LoginResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default);

    Task<MfaChallengeResult> BeginMfaAsync(BeginMfaRequest request, CancellationToken ct = default);

    Task<LoginResult> CompleteMfaAsync(CompleteMfaRequest request, CancellationToken ct = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);

    Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default);

    Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default);
}

internal interface IUAuthInternalFlowService
{
    Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, LoginExecutionOptions loginExecution, CancellationToken ct = default);
    Task<LoginResult> LoginAsync(AuthFlowContext flow, AuthExecutionContext execution, LoginRequest request, LoginExecutionOptions loginExecution, CancellationToken ct = default);
}
