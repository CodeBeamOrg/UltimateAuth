using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestPasswordHasher : IUAuthPasswordHasher
{
    public PasswordHash Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new UAuthValidationException("password_required");

        return PasswordHash.Create(PasswordAlgorithms.Legacy, $"TEST::{password}");
    }

    public bool Verify(PasswordHash hash, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(hash.Hash))
            return false;

        if (hash.Algorithm != PasswordAlgorithms.Legacy)
            return false;

        return hash.Hash == $"TEST::{secret}";
    }

    public bool NeedsRehash(PasswordHash hash)
    {
        return false;
    }
}
