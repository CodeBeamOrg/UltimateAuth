using CodeBeam.UltimateAuth.Credentials;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Infrastructure;

internal sealed class DevelopmentCredentialResetNotifier(ILogger<DevelopmentCredentialResetNotifier> logger) : ICredentialResetNotifier
{
    public Task NotifyAsync(CredentialResetNotification notification, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogWarning(
            "DEVELOPMENT ONLY — Credential reset for tenant {Tenant}, " +
            "user {UserKey}. Code: {Code}. Expires at: {ExpiresAt}",
            notification.Tenant,
            notification.UserKey,
            notification.Token,
            notification.ExpiresAt);

        return Task.CompletedTask;
    }
}
