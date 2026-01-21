namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record AddCredentialResult(
        bool Succeeded,
        string? Error,
        CredentialType? Type = null)
    {
        public static AddCredentialResult Success(CredentialType type)
            => new(true, null, type);

        public static AddCredentialResult Fail(string error)
            => new(false, error);
    }
}
