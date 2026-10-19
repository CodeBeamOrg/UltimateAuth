namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Evaluates UltimateAuth authorization requirements.
/// Implementations should not manage component lifecycle or rendering.
/// </summary>
public interface IUAuthAuthorizationEvaluator
{
    Task<UAuthAuthorizationResult> EvaluateAsync(UAuthState state, UAuthAuthorizationRequirement requirement);
}
