namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record DisableMfaRequest
{
    public MfaMethod? Method { get; init; } // null = all
}
