using CodeBeam.UltimateAuth.Core.Contracts;

// This is a seperate service because validation runs only once before AuthFlowContext is created.
namespace CodeBeam.UltimateAuth.Server;

public interface ISessionValidator
{
    /// <summary>
    /// Validates a session for runtime authentication.
    /// Hot path – must be fast and side-effect free.
    /// </summary>
    Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default);
}
