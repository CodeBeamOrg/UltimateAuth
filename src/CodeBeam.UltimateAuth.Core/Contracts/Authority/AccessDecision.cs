namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AccessDecision
{
    public bool IsAllowed { get; }
    public bool RequiresReauthentication { get; }
    public string? DenyReason { get; }

    private AccessDecision(bool isAllowed, bool requiresReauthentication, string? denyReason)
    {
        IsAllowed = isAllowed;
        RequiresReauthentication = requiresReauthentication;
        DenyReason = denyReason;
    }

    public static AccessDecision Allow()
        => new(
            isAllowed: true,
            requiresReauthentication: false,
            denyReason: null);

    public static AccessDecision Deny(string reason)
        => new(
            isAllowed: false,
            requiresReauthentication: false,
            denyReason: reason);

    public static AccessDecision ReauthenticationRequired(string? reason = null)
        => new(
            isAllowed: false,
            requiresReauthentication: true,
            denyReason: reason);

    public bool IsDenied =>
        !IsAllowed && !RequiresReauthentication;
}
