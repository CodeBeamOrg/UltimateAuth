namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Represents the outcome of a login decision.
/// </summary>
public sealed class LoginDecision
{
    public LoginDecisionKind Kind { get; }
    public string? Reason { get; }

    private LoginDecision(LoginDecisionKind kind, string? reason = null)
    {
        Kind = kind;
        Reason = reason;
    }

    public static LoginDecision Allow()
        => new(LoginDecisionKind.Allow);

    public static LoginDecision Deny(string reason)
        => new(LoginDecisionKind.Deny, reason);

    public static LoginDecision Challenge(string reason)
        => new(LoginDecisionKind.Challenge, reason);
}
