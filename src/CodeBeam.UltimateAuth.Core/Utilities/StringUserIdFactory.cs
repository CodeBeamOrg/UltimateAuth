using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Utilities
{
    public sealed class StringUserIdFactory : IUserIdFactory<string>
    {
        public string Create() => Guid.NewGuid().ToString("N");
    }
}
