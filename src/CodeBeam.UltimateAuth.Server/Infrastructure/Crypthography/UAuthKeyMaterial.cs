using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class UAuthKeyMaterial
{
    public static byte[] Decode(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} must not be null or empty.");
        }

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"{name} must be a valid Base64-encoded key.", ex);
        }

        if (bytes.Length < 32)
        {
            throw new InvalidOperationException($"{name} must contain at least 32 bytes.");
        }

        if (bytes.All(x => x == bytes[0]))
        {
            throw new InvalidOperationException($"{name} contains obviously weak key material.");
        }

        return bytes;
    }

    public static void EnsureDistinct(byte[] first, byte[] second)
    {
        if (first.Length == second.Length && CryptographicOperations.FixedTimeEquals(first, second))
        {
            throw new InvalidOperationException("JWT signing and token hashing keys must be different.");
        }
    }
}
