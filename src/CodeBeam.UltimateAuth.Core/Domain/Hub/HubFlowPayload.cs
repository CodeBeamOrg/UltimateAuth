namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubFlowPayload
{
    private readonly Dictionary<string, object?> _items = new();

    public IReadOnlyDictionary<string, object?> Items => _items;

    public void Set<T>(string key, T value) => _items[key] = value;

    public bool TryGet<T>(string key, out T? value)
    {
        if (_items.TryGetValue(key, out var raw) && raw is T t)
        {
            value = t;
            return true;
        }

        value = default;
        return false;
    }
}
