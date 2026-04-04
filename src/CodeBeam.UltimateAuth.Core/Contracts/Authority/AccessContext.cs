using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
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
    public SessionChainId? ActorChainId { get; }

    // Target
    public string? Resource { get; init; }
    public UserKey? TargetUserKey { get; init; }
    public TenantKey ResourceTenant { get; init; }

    public string Action { get; init; } = default!;
    public IReadOnlyDictionary<string, object> Attributes { get; init; } = EmptyAttributes.Instance;

    public bool IsCrossTenant => !string.Equals(ActorTenant, ResourceTenant, StringComparison.Ordinal);
    public bool IsSelfAction => ActorUserKey != null && TargetUserKey != null && string.Equals(ActorUserKey.Value, TargetUserKey.Value, StringComparison.Ordinal);
    public bool HasActor => ActorUserKey != null;
    public bool HasTarget => TargetUserKey != null;

    public UserKey GetTargetUserKey()
    {
        if (TargetUserKey is not UserKey targetUserKey)
            throw new UAuthNotFoundException("Target user is not found.");

        return targetUserKey;
    }

    internal AccessContext(
        UserKey? actorUserKey,
        TenantKey actorTenant,
        bool isAuthenticated,
        bool isSystemActor,
        SessionChainId? actorChainId,
        string? resource,
        UserKey? targetUserKey,
        TenantKey resourceTenant,
        string action,
        IReadOnlyDictionary<string, object> attributes)
    {
        ActorUserKey = actorUserKey;
        ActorTenant = actorTenant;
        IsAuthenticated = isAuthenticated;
        IsSystemActor = isSystemActor;
        ActorChainId = actorChainId;

        Resource = resource;
        TargetUserKey = targetUserKey;
        ResourceTenant = resourceTenant;

        Action = action;
        Attributes = attributes;
    }

    public AccessContext WithAttribute(string key, object value)
    {
        var merged = new Dictionary<string, object>(Attributes)
        {
            [key] = value
        };

        return new AccessContext(
            ActorUserKey,
            ActorTenant,
            IsAuthenticated,
            IsSystemActor,
            ActorChainId,
            Resource,
            TargetUserKey,
            ResourceTenant,
            Action,
            merged
        );
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
