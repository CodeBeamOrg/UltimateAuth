namespace CodeBeam.UltimateAuth.IdentityLab.Services;

internal static class LabProvisioningGate
{
    internal const string AttributeKey = "identitylab.internal.provisioning";

    // Reference identity is intentional.
    // A string, boolean or client-supplied value cannot reproduce this token.
    internal static readonly object Token = new();

    internal static bool IsTrusted(IReadOnlyDictionary<string, object> attributes)
    {
        return attributes.TryGetValue(AttributeKey, out var value) && ReferenceEquals(value, Token);
    }
}
