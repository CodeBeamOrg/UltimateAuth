namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthChallengeRequiredException : UAuthException
{
    public UAuthChallengeRequiredException(string? reason = null) 
        : base(code: "challenge_required", message: reason ?? "Additional authentication is required.")
    {
    }
}
