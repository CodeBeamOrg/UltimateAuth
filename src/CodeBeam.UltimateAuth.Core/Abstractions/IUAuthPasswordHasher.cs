namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Securely hashes and verifies user passwords.
    /// Designed for slow, adaptive, memory-hard algorithms
    /// such as Argon2 or bcrypt.
    /// </summary>
    public interface IUAuthPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}
