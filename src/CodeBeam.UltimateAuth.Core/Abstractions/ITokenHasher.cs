namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Hashes and verifies sensitive tokens.
    /// Used for refresh tokens, session ids, opaque tokens.
    /// </summary>
    public interface ITokenHasher
    {
        string Hash(string plaintext);
        bool Verify(string plaintext, string hash);
    }
}
