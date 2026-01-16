namespace CodeBeam.UltimateAuth.Users;

public sealed record UserAccessDecision(
    bool IsAllowed,
    bool RequiresReauthentication,
    string? DenyReason = null);
