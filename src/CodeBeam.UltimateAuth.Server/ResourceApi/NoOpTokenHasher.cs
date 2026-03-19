using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class NoOpTokenHasher : ITokenHasher
{
    public string Hash(string input) => input;
    public bool Verify(string input, string hash) => input == hash;
}
