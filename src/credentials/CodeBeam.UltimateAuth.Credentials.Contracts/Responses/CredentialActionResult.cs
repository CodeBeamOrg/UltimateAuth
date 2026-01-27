namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record CredentialActionResult(
        bool Succeeded,
        string? Error)
    {
        public static CredentialActionResult Success()
            => new(true, null);

        public static CredentialActionResult Fail(string error)
            => new(false, error);
    }
}
