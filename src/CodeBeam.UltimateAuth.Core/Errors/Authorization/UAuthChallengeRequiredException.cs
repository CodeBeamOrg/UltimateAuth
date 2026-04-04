namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthChallengeRequiredException : UAuthRuntimeException
{
    public override int StatusCode => 401;

    public override string Title => "Reauthentication Required";

    public override string TypePrefix => "https://docs.ultimateauth.com/errors/challenge";

    public UAuthChallengeRequiredException(string? reason = null)
        : base("challenge_required", reason ?? "Additional authentication is required.")
    {
    }
}
