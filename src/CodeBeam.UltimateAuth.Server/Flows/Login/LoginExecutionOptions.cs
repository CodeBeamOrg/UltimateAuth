namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed record LoginExecutionOptions
{
    public LoginExecutionMode Mode { get; init; } = LoginExecutionMode.Commit;
    public bool SuppressFailureAttempt { get; init; }
    public bool SuppressSuccessReset { get; init; }
}
