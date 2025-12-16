using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class StringUserIdFactory : IUserIdFactory<string>
    {
        public string Create() => Guid.NewGuid().ToString("N");
    }
}
