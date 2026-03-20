using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NotSupportedPasswordHasher : IUAuthPasswordHasher
{
    public string Hash(string password)
    {
        throw new NotSupportedException();
    }

    public bool Verify(string hash, string secret)
    {
        throw new NotSupportedException();
    }
}
