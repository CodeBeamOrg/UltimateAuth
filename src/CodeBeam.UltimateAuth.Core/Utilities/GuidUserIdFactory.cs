using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Utilities
{
    public sealed class GuidUserIdFactory : IUserIdFactory<Guid>
    {
        public Guid Create() => Guid.NewGuid();
    }
}
