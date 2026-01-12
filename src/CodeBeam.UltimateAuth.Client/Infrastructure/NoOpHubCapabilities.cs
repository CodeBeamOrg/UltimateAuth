using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal sealed class NoOpHubCapabilities : IHubCapabilities
    {
        public bool SupportsPkce => false;
    }
}
