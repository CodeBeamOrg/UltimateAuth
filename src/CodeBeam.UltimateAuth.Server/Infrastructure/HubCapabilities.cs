using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class HubCapabilities : IHubCapabilities
    {
        public bool SupportsPkce => true;
    }
}
