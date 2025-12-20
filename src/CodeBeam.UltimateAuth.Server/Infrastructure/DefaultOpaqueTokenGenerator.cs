using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultOpaqueTokenGenerator : IOpaqueTokenGenerator
    {
        public string Generate(int bytes)
            => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(bytes));
    }

}
