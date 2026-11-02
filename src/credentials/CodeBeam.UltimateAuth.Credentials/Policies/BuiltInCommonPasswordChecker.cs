using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

/// <summary>
/// Lightweight, offline common-password checker.
/// Provides baseline protection without an external dataset.
/// </summary>
public sealed class BuiltInCommonPasswordChecker : IPasswordCommonPasswordChecker
{
    private static readonly HashSet<string> BasicPasswords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "123456",
            "12345678",
            "123456789",
            "1234567890",
            "12345",
            "1234567",
            "12345678910",
            "111111",
            "000000",
            "qwerty",
            "qwerty123",
            "abc123",
            "admin",
            "admin123",
            "letmein",
            "welcome",
            "iloveyou",
            "password1",
            "password123"
        };

    private static readonly HashSet<string> StandardPasswords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "123123",
            "654321",
            "112233",
            "121212",
            "123321",
            "qwertyuiop",
            "asdfgh",
            "asdfghjkl",
            "zxcvbn",
            "1q2w3e4r",
            "1q2w3e4r5t",
            "1qaz2wsx",
            "zaq12wsx",
            "passw0rd",
            "p@ssw0rd",
            "p@ssword",
            "password!",
            "password@123",
            "welcome123",
            "administrator",
            "root123",
            "test123",
            "changeme",
            "default",
            "secret",
            "secret123",
            "football",
            "baseball",
            "monkey",
            "dragon",
            "sunshine",
            "princess",
            "superman",
            "master",
            "trustno1"
        };

    private static readonly HashSet<string> StrictPasswords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "password123!",
            "password@123!",
            "admin123!",
            "welcome123!",
            "qwerty123!",
            "letmein123",
            "administrator123",
            "passw0rd123",
            "p@ssw0rd123",
            "iloveyou123",
            "123456789abc",
            "abc123456789",
            "qwerty123456",
            "asdfgh123456",
            "changeme123",
            "defaultpassword",
            "temporarypassword",
            "temporary123",
            "mysecretpassword",
            "supersecret",
            "password2024",
            "password2025",
            "password2026"
        };

    public Task<bool> IsCommonAsync(string password, PasswordPolicyLevel level, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(password);

        var result = level switch
        {
            PasswordPolicyLevel.Disabled => false,

            PasswordPolicyLevel.Basic =>
                BasicPasswords.Contains(password),

            PasswordPolicyLevel.Standard =>
                BasicPasswords.Contains(password) ||
                StandardPasswords.Contains(password),

            PasswordPolicyLevel.Strict =>
                BasicPasswords.Contains(password) ||
                StandardPasswords.Contains(password) ||
                StrictPasswords.Contains(password),

            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        return Task.FromResult(result);
    }
}
