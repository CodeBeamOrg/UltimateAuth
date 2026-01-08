using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultUAuthHeaderPolicyBuilder : IUAuthHeaderPolicyBuilder
    {
        public string BuildHeaderValue(string rawValue, CredentialResponseOptions response, AuthFlowContext context)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                throw new ArgumentException("Header value cannot be empty.", nameof(rawValue));

            return response.HeaderFormat switch
            {
                HeaderTokenFormat.Bearer => $"Bearer {rawValue}",
                HeaderTokenFormat.Raw => rawValue,

                _ => throw new InvalidOperationException($"Unsupported header token format: {response.HeaderFormat}")
            };
        }
    }
}
