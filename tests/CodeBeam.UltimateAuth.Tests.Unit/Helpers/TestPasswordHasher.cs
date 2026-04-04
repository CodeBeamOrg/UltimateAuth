using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestPasswordHasher : IUAuthPasswordHasher
{
    public string Hash(string password) => $"HASH::{password}";
    public bool Verify(string hashedPassword, string providedPassword) => hashedPassword == $"HASH::{providedPassword}";
}
