using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public readonly record struct Permission
{
    public string Value { get; }

    [JsonConstructor]
    public Permission(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("permission_required", nameof(value));

        Value = value.Trim().ToLowerInvariant();
    }

    public static Permission From(string value) => new(value);

    public static readonly Permission Wildcard = new("*");

    public bool IsWildcard => Value == "*";

    public bool IsPrefix => Value?.EndsWith(".*", StringComparison.Ordinal) == true;

    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public override string ToString() => Value ?? string.Empty;

    public static implicit operator string(Permission p) => p.Value;
}
