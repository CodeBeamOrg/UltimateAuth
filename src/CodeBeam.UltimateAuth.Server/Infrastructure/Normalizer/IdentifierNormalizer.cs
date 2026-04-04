using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class IdentifierNormalizer : IIdentifierNormalizer
{
    private readonly UAuthIdentifierNormalizationOptions _options;

    public IdentifierNormalizer(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.LoginIdentifiers.Normalization;
    }

    public NormalizedIdentifier Normalize(UserIdentifierType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new(value, string.Empty, false, "identifier_empty");

        var raw = value;
        var normalized = BasicNormalize(value);

        return type switch
        {
            UserIdentifierType.Email => NormalizeEmail(raw, normalized),
            UserIdentifierType.Phone => NormalizePhone(raw, normalized),
            UserIdentifierType.Username => NormalizeUsername(raw, normalized),
            _ => NormalizeCustom(raw, normalized)
        };
    }

    private static string BasicNormalize(string value)
    {
        var form = value.Normalize(NormalizationForm.FormKC).Trim();

        var sb = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (char.IsControl(ch))
                continue;

            if (ch is '\u200B' or '\u200C' or '\u200D' or '\uFEFF')
                continue;

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private NormalizedIdentifier NormalizeUsername(string raw, string value)
    {
        if (value.Length < 3 || value.Length > 256)
            return new(raw, value, false, "username_invalid_length");

        value = ApplyCasePolicy(value, _options.UsernameCase);

        return new(raw, value, true, null);
    }

    private NormalizedIdentifier NormalizeEmail(string raw, string value)
    {
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex != value.LastIndexOf('@'))
            return new(raw, value, false, "email_invalid_format");

        var local = value[..atIndex];
        var domain = value[(atIndex + 1)..];

        if (string.IsNullOrWhiteSpace(domain) || !domain.Contains('.'))
            return new(raw, value, false, "email_invalid_domain");

        try
        {
            var idn = new IdnMapping();
            domain = idn.GetAscii(domain);
        }
        catch
        {
            return new(raw, value, false, "email_invalid_domain");
        }

        var normalized = $"{local}@{domain}";
        normalized = ApplyCasePolicy(normalized, _options.EmailCase);

        return new(raw, normalized, true, null);
    }

    private NormalizedIdentifier NormalizePhone(string raw, string value)
    {
        var sb = new StringBuilder();

        foreach (var ch in value)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
            else if (ch == '+' && sb.Length == 0)
                sb.Append(ch);
        }

        var digits = sb.ToString();

        if (digits.Length < 7)
            return new(raw, digits, false, "phone_invalid_length");

        return new(raw, digits, true, null);
    }

    private NormalizedIdentifier NormalizeCustom(string raw, string value)
    {
        value = ApplyCasePolicy(value, _options.CustomCase);

        if (value.Length == 0)
            return new(raw, value, false, "identifier_invalid");

        return new(raw, value, true, null);
    }

    private static string ApplyCasePolicy(string value, CaseHandling policy)
    {
        return policy switch
        {
            CaseHandling.ToLower => value.ToLowerInvariant(),
            CaseHandling.ToUpper => value.ToUpperInvariant(),
            _ => value
        };
    }
}
