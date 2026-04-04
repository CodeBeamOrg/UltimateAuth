namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public static class PermissionExpander
{
    public static IReadOnlyCollection<Permission> Expand(IEnumerable<Permission> stored, IEnumerable<string> catalog)
    {
        var result = new HashSet<string>();

        foreach (var perm in stored)
        {
            if (perm.IsWildcard)
            {
                result.UnionWith(catalog);
                continue;
            }

            if (perm.IsPrefix)
            {
                var prefix = perm.Value[..^2];
                result.UnionWith(catalog.Where(x => x.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)));
                continue;
            }

            result.Add(perm.Value);
        }

        return result.Select(Permission.From).ToArray();
    }
}
