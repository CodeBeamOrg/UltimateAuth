using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Represents the outcome of a login decision.
/// </summary>
public sealed class LoginDecision
{
    public LoginDecisionKind Kind { get; }
    public AuthFailureReason? FailureReason { get; }


    private LoginDecision(LoginDecisionKind kind, AuthFailureReason? reason = null)
    {
        Kind = kind;
        FailureReason = reason;
    }

    public static LoginDecision Allow()
        => new(LoginDecisionKind.Allow);

    public static LoginDecision Deny(AuthFailureReason reason)
        => new(LoginDecisionKind.Deny, reason);

    public static LoginDecision Challenge(AuthFailureReason reason)
        => new(LoginDecisionKind.Challenge, reason);
}
