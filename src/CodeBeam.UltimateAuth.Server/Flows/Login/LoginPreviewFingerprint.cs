using System.Security.Cryptography;
using System.Text;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal static class LoginPreviewFingerprint
{
    public static string Create(TenantKey tenant, string identifier, CredentialType factor, string secret, DeviceId deviceId)
    {
        var normalized = $"{tenant.Value}|{identifier}|{factor}|{deviceId.Value}|{secret}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}