namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class NotConfiguredUserIdentifierVerifier : IUserIdentifierVerifier
{
    internal static InvalidOperationException CreateException() => new(
        "User identifier verification is not configured. " +
        "Implement IUserIdentifierVerifier and register it with " +
        "services.AddScoped<IUserIdentifierVerifier, " +
        "YourUserIdentifierVerifier>(). " +
        "The implementation must validate ownership proof for the " +
        "specified tenant, user, identifier and current value.");

    public Task<bool> VerifyAsync(UserIdentifierVerificationContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        throw CreateException();
    }
}
