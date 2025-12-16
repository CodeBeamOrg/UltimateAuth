using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class GuidUserIdFactory : IUserIdFactory<Guid>
    {
        public Guid Create() => Guid.NewGuid();
    }
}
