namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record BeginMfaSetupRequest
    {
        public MfaMethod Method { get; init; }
    }
}
