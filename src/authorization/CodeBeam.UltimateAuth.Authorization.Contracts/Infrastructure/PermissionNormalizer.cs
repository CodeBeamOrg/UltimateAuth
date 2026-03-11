namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public static class PermissionNormalizer
{
    public static IReadOnlyCollection<Permission> Normalize(IEnumerable<Permission> permissions, IEnumerable<string> catalog)
    {
        var selected = new HashSet<string>(permissions.Select(p => p.Value), StringComparer.OrdinalIgnoreCase);

        if (selected.Contains("*"))
            return new[] { Permission.Wildcard };

        if (selected.Count == catalog.Count() && catalog.All(selected.Contains))
        {
            return new[] { Permission.Wildcard };
        }

        var result = new HashSet<string>();

        var catalogGroups = catalog.GroupBy(p => p.Split('.')[0]);

        foreach (var group in catalogGroups)
        {
            var prefix = group.Key;

            var allPermissions = group.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var selectedInGroup = selected
                .Where(p => p.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (selectedInGroup.SetEquals(allPermissions))
            {
                result.Add(prefix + ".*");
            }
            else
            {
                result.UnionWith(selectedInGroup);
            }
        }

        return result.Select(Permission.From).ToArray();
    }
}
