namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record BeginMfaSetupResult
    {
        public MfaMethod Method { get; init; }
        public string? SharedSecret { get; init; }     // TOTP
        public string? QrCodeUri { get; init; }        // TOTP
    }
}
