namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserAccessDecision(
    bool IsAllowed,
    bool RequiresReauthentication,
    string? DenyReason = null);
