using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class AccessContext
{
    // Actor
    public UserKey? ActorUserKey { get; init; }
    public TenantKey ActorTenant { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool IsSystemActor { get; init; }

    // Target
    public string? Resource { get; init; }
    public string? ResourceId { get; init; }
    public TenantKey ResourceTenant { get; init; }

    public string Action { get; init; } = default!;
    public IReadOnlyDictionary<string, object> Attributes { get; init; } = EmptyAttributes.Instance;

    public bool IsCrossTenant => !string.Equals(ActorTenant, ResourceTenant, StringComparison.Ordinal);
    public bool IsSelfAction => ActorUserKey != null && ResourceId != null && string.Equals(ActorUserKey.Value, ResourceId, StringComparison.Ordinal);
    public bool HasActor => ActorUserKey != null;
    public bool HasTarget => ResourceId != null;

    public UserKey GetTargetUserKey()
    {
        if (ResourceId is null)
            throw new InvalidOperationException("Target user is not specified.");

        return UserKey.Parse(ResourceId, null);
    }
}

internal sealed class EmptyAttributes : IReadOnlyDictionary<string, object>
{
    public static readonly EmptyAttributes Instance = new();

    private EmptyAttributes() { }

    public IEnumerable<string> Keys => Array.Empty<string>();
    public IEnumerable<object> Values => Array.Empty<object>();
    public int Count => 0;
    public object this[string key] => throw new KeyNotFoundException();
    public bool ContainsKey(string key) => false;
    public bool TryGetValue(string key, out object value)
    {
        value = default!;
        return false;
    }
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
