using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialResetNotifier
{
    Task NotifyAsync(CredentialResetNotification notification, CancellationToken ct = default);
}

public sealed record CredentialResetNotification(
    TenantKey Tenant,
    UserKey UserKey,
    CredentialType CredentialType,
    ResetCodeType CodeType,
    string Token,
    DateTimeOffset ExpiresAt);
