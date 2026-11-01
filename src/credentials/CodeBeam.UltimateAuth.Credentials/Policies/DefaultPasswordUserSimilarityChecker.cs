using System.Text;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public sealed class DefaultPasswordUserSimilarityChecker : IPasswordUserSimilarityChecker
{
    public bool IsSimilar(PasswordPolicyContext context, PasswordPolicyLevel level)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (level == PasswordPolicyLevel.Disabled)
            return false;

        if (level is not (
            PasswordPolicyLevel.Basic or
            PasswordPolicyLevel.Standard or
            PasswordPolicyLevel.Strict))
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        var password = context.Password;

        if (string.IsNullOrEmpty(password))
            return false;

        var candidates = GetCandidates(context);

        foreach (var candidate in candidates)
        {
            if (string.Equals(password, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (level >= PasswordPolicyLevel.Standard && candidate.Length >= 4 && password.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (level == PasswordPolicyLevel.Strict && candidate.Length >= 4 && IsStrictMatch(password, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCandidates(PasswordPolicyContext context)
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(context.UserName))
            values.Add(context.UserName);

        values.AddRange(context.UserIdentifiers ?? Array.Empty<string>());

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var trimmed = value.Trim();

            if (trimmed.Length == 0)
                continue;

            candidates.Add(trimmed);

            var atIndex = trimmed.IndexOf('@');

            if (atIndex > 0)
                candidates.Add(trimmed[..atIndex]);
        }

        return candidates;
    }

    private static bool IsStrictMatch(string password, string candidate)
    {
        var normalizedPassword = Normalize(password);
        var normalizedCandidate = Normalize(candidate);

        return normalizedCandidate.Length >= 4 && normalizedPassword.Contains(normalizedCandidate, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var c in value.ToLowerInvariant())
        {
            var normalized = c switch
            {
                '0' => 'o',
                '1' => 'i',
                '3' => 'e',
                '4' => 'a',
                '5' => 's',
                '7' => 't',
                '@' => 'a',
                '$' => 's',
                _ => c
            };

            if (char.IsLetterOrDigit(normalized))
                builder.Append(normalized);
        }

        return builder.ToString();
    }
}
