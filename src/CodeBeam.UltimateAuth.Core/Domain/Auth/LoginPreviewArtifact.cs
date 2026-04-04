using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class LoginPreviewArtifact : AuthArtifact
{
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }
    public CredentialType Factor { get; }
    public string DeviceId { get; }
    public string Identifier { get; }
    public UAuthClientProfile ClientProfile { get; }
    public string Fingerprint { get; }

    public LoginPreviewArtifact(
        TenantKey tenant,
        UserKey userKey,
        CredentialType factor,
        string deviceId,
        string identifier,
        UAuthClientProfile clientProfile,
        string fingerprint,
        DateTimeOffset expiresAt)
        : base(AuthArtifactType.LoginPreview, expiresAt)
    {
        Tenant = tenant;
        UserKey = userKey;
        Factor = factor;
        DeviceId = deviceId;
        Identifier = identifier;
        ClientProfile = clientProfile;
        Fingerprint = fingerprint;
    }

    public bool Matches(
        TenantKey tenant,
        UserKey userKey,
        CredentialType factor,
        string deviceId,
        string identifier,
        UAuthClientProfile clientProfile,
        string fingerprint)
    {
        return Tenant == tenant
            && UserKey == userKey
            && Factor == factor
            && string.Equals(DeviceId, deviceId, StringComparison.Ordinal)
            && string.Equals(Identifier, identifier, StringComparison.Ordinal)
            && ClientProfile == clientProfile
            && string.Equals(Fingerprint, fingerprint, StringComparison.Ordinal);
    }
}