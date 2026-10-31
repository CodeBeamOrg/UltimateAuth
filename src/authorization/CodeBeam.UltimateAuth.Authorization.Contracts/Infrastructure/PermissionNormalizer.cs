namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public static class PermissionNormalizer
{
    public static IReadOnlyCollection<Permission> Normalize(IEnumerable<Permission> permissions, IEnumerable<string> catalog)
    {
        var selected = new HashSet<string>(
            permissions.Select(p => p.Value),
            StringComparer.OrdinalIgnoreCase);

        if (selected.Contains("*"))
            return [Permission.Wildcard];

        var catalogItems = catalog.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        // Only collapse to "*" when there are no additional
        // permissions outside the known catalog.
        if (selected.SetEquals(catalogItems))
            return [Permission.Wildcard];

        // Preserve all permissions, including custom ones.
        var result = new HashSet<string>(
            selected,
            StringComparer.OrdinalIgnoreCase);

        var catalogGroups = catalogItems.GroupBy(
            p => p.Split('.')[0]);

        foreach (var group in catalogGroups)
        {
            var prefix = group.Key;

            var groupPermissions = group.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

            var selectedInGroup = selected
                .Where(p => groupPermissions.Contains(p))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (selectedInGroup.SetEquals(groupPermissions))
            {
                result.ExceptWith(groupPermissions);
                result.Add(prefix + ".*");
            }
        }

        return result
            .Select(Permission.From)
            .ToArray();
    }
}
