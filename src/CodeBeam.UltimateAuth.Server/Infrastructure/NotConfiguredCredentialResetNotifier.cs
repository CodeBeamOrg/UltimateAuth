using CodeBeam.UltimateAuth.Credentials;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class NotConfiguredCredentialResetNotifier : ICredentialResetNotifier
{
    internal static InvalidOperationException CreateException() => new(
        "Credential reset notification is not configured. " +
        "Implement ICredentialResetNotifier and register it with " +
        "services.AddScoped<ICredentialResetNotifier, " +
        "YourCredentialResetNotifier>(). " +
        "The implementation must send the reset code or token " +
        "through a trusted channel associated with the user's account.");

    public Task NotifyAsync(CredentialResetNotification context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        throw CreateException();
    }
}
