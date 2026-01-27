namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record RevokeCredentialRequest(
        DateTimeOffset? Until = null,
        string? Reason = null);
}
