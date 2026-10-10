using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Security;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

[JsonConverter(typeof(TenantKeyJsonConverter))]
public readonly record struct TenantKey : IParsable<TenantKey>
{
    public string Value { get; }

    private TenantKey(string value)
    {
        Value = value;
    }

    private static readonly Regex Allowed = new(@"^[a-zA-Z0-9][a-zA-Z0-9._-]{0,63}$", RegexOptions.Compiled);

    internal static readonly TenantKey Single = new("__single__");
    internal static readonly TenantKey System = new("__system__");
    internal static readonly TenantKey Unresolved = new("__unresolved__");

    public bool IsSingle => Value == Single.Value;
    public bool IsSystem => Value == System.Value;
    public bool IsUnresolved => Value == Unresolved.Value;
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// True only for real, customer-defined tenants.
    /// </summary>
    public bool IsNormal => IsValid && !IsSingle && !IsSystem && !IsUnresolved;

    public static TenantKey Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid TenantKey value: '{s}'");

        return result;
    }

    public static TenantKey Parse(string value) => Parse(value, null);

    public static bool TryParse(string? s, IFormatProvider? provider, out TenantKey result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            result = FromExternal(s);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a tenant key from EXTERNAL input (HTTP, headers, tokens).
    /// System-reserved values are rejected.
    /// </summary>
    public static TenantKey FromExternal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new SecurityException("Missing tenant claim.");

        var normalized = Normalize(value);

        if (normalized == Single.Value || normalized == System.Value || normalized == Unresolved.Value)
        {
            throw new ArgumentException("Reserved tenant id.");
        }

        return new TenantKey(normalized);
    }

    /// <summary>
    /// Internal creation for framework use only.
    /// </summary>
    internal static TenantKey FromInternal(string value)
    {
        if (value == Single.Value || value == System.Value || value == Unresolved.Value)
        {
            return new TenantKey(value);
        }

        return new TenantKey(Normalize(value));
    }

    private static string Normalize(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var normalized = value.Trim();

        if (normalized.Length == 0)
            throw new ArgumentException("TenantKey cannot be empty.");

        if (normalized.Length > 128)
            throw new ArgumentException("TenantKey is too long.");

        if (!Allowed.IsMatch(normalized))
            throw new ArgumentException("TenantKey contains invalid characters.");

        return normalized;
    }

    public override string ToString() => Value;

    public static implicit operator string(TenantKey key) => key.Value;
}
