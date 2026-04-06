namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Securely hashes and verifies user passwords.
/// Designed for slow, adaptive, memory-hard algorithms
/// such as Argon2 or bcrypt.
/// </summary>
public interface IUAuthPasswordHasher
{
    PasswordHash Hash(string password);
    bool Verify(PasswordHash hash, string secret);
    bool NeedsRehash(PasswordHash hash);
}
