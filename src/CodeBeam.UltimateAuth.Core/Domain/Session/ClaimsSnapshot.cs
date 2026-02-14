using System.Security.Claims;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain;

/// <summary>
/// Represents a deterministic, immutable collection of authorization claims.
///
/// This object contains only security-related claims such as:
/// - Roles
/// - Permissions
/// - Policy markers.
///
/// It must not contain profile data, display data or identity metadata.
/// </summary>
public sealed class ClaimsSnapshot
{
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _claims;
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Claims => _claims;

    [JsonConstructor]
    public ClaimsSnapshot(IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims)
    {
        _claims = claims;
    }

    public static ClaimsSnapshot Empty { get; } = new(new Dictionary<string, IReadOnlyCollection<string>>());

    public string? Get(string type) => _claims.TryGetValue(type, out var values) ? values.FirstOrDefault() : null;
    public bool TryGet(string type, out string value)
    {
        value = null!;

        if (!Claims.TryGetValue(type, out var values))
            return false;

        var first = values.FirstOrDefault();
        if (first is null)
            return false;

        value = first;
        return true;
    }
    public IReadOnlyCollection<string> GetAll(string type) => _claims.TryGetValue(type, out var values) ? values : Array.Empty<string>();

    public bool Has(string type) => _claims.ContainsKey(type);
    public bool HasValue(string type, string value) => _claims.TryGetValue(type, out var values) && values.Contains(value);

    public IReadOnlyCollection<string> Roles => GetAll(ClaimTypes.Role);
    public IReadOnlyCollection<string> Permissions => GetAll("uauth:permission");

    public bool IsInRole(string role) => HasValue(ClaimTypes.Role, role);
    public bool HasPermission(string permission) => HasValue("uauth:permission", permission);

    /// <summary>
    /// Flattens claims by taking the first value of each claim.
    /// Useful for logging, diagnostics, or legacy consumers.
    /// </summary>
    public IReadOnlyDictionary<string, string> AsDictionary()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (type, values) in Claims)
        {
            var first = values.FirstOrDefault();
            if (first is not null)
                dict[type] = first;
        }

        return dict;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ClaimsSnapshot other)
            return false;

        if (Claims.Count != other.Claims.Count)
            return false;

        foreach (var (type, values) in Claims)
        {
            if (!other.Claims.TryGetValue(type, out var otherValues))
                return false;

            if (values.Count != otherValues.Count)
                return false;

            if (!values.All(v => otherValues.Contains(v)))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            foreach (var (type, values) in Claims.OrderBy(x => x.Key))
            {
                hash = hash * 23 + type.GetHashCode();

                foreach (var value in values.OrderBy(v => v))
                {
                    hash = hash * 23 + value.GetHashCode();
                }
            }

            return hash;
        }
    }

    public static ClaimsSnapshot From(params (string Type, string Value)[] claims)
    {
        var dict = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (type, value) in claims)
        {
            if (!dict.TryGetValue(type, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                dict[type] = set;
            }

            set.Add(value);
        }

        return new ClaimsSnapshot(dict.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value.ToArray(), StringComparer.Ordinal));
    }

    public ClaimsSnapshot With(params (string Type, string Value)[] claims)
    {
        if (claims.Length == 0)
            return this;

        var dict = Claims.ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value, StringComparer.Ordinal), StringComparer.Ordinal);

        foreach (var (type, value) in claims)
        {
            if (!dict.TryGetValue(type, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                dict[type] = set;
            }

            set.Add(value);
        }

        return new ClaimsSnapshot(dict.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value.ToArray(), StringComparer.Ordinal));
    }

    public ClaimsSnapshot Merge(ClaimsSnapshot other)
    {
        if (other is null || other.Claims.Count == 0)
            return this;

        if (Claims.Count == 0)
            return other;

        var dict = Claims.ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value, StringComparer.Ordinal), StringComparer.Ordinal);

        foreach (var (type, values) in other.Claims)
        {
            if (!dict.TryGetValue(type, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                dict[type] = set;
            }

            foreach (var value in values)
                set.Add(value);
        }

        return new ClaimsSnapshot(dict.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value.ToArray(), StringComparer.Ordinal));
    }

    public static ClaimsSnapshotBuilder Create() => new ClaimsSnapshotBuilder();

}
