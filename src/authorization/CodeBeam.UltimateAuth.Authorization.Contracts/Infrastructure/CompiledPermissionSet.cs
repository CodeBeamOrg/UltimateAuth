namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed class CompiledPermissionSet
{
    private readonly HashSet<string> _exact = new();
    private readonly HashSet<string> _prefix = new();
    private readonly bool _hasWildcard;

    public CompiledPermissionSet(IEnumerable<Permission> permissions)
    {
        foreach (var p in permissions)
        {
            var value = p.Value;

            if (value == "*")
            {
                _hasWildcard = true;
                continue;
            }

            if (value.EndsWith(".*"))
            {
                _prefix.Add(value[..^2]);
                continue;
            }

            _exact.Add(value);
        }
    }

    public bool IsAllowed(string action)
    {
        if (_hasWildcard)
            return true;

        if (_exact.Contains(action))
            return true;

        foreach (var prefix in _prefix)
        {
            if (action.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
