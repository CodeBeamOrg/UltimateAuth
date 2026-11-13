using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class LoginPasswordVerificationState
{
    private readonly object _sync = new();
    private PasswordHash? _dummyHash;

    internal PasswordHash GetDummyHash(IUAuthPasswordHasher hasher)
    {
        lock (_sync)
        {
            if (_dummyHash is { } existing)
                return existing;

            var dummySecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var hash = hasher.Hash(dummySecret);

            _dummyHash = hash;
            return hash;
        }
    }
}
