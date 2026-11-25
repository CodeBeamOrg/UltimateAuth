using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class UAuthHeaderPolicyBuilder : IUAuthHeaderPolicyBuilder
{
    public string BuildHeaderValue(string rawValue, EffectiveGrantDelivery delivery, AuthFlowContext context)
    {
        if (delivery.Mode != GrantDeliveryMode.Header)
        {
            throw new InvalidOperationException($"Header policy requested for non-header grant '{delivery.Kind}'.");
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new ArgumentException("Header value cannot be empty.", nameof(rawValue));
        }

        return delivery.HeaderFormat switch
        {
            HeaderTokenFormat.Bearer => $"Bearer {rawValue}",
            HeaderTokenFormat.Raw => rawValue,
            _ => throw new InvalidOperationException($"Unsupported header token format: {delivery.HeaderFormat}")
        };
    }
}
