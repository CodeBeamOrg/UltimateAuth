using CodeBeam.UltimateAuth.Core.Defaults;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public static class UAuthPermissionCatalog
{
    public static IReadOnlyList<string> GetAll()
    {
        var result = new List<string>();
        Collect(typeof(UAuthActions), result);
        return result;
    }

    public static IReadOnlyList<string> GetAdminPermissions()
    {
        return GetAll()
            .Where(x => x.EndsWith(".admin", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static void Collect(Type type, List<string> result)
    {
        foreach (var field in type.GetFields(
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.FlattenHierarchy))
        {
            if (field.IsLiteral && field.FieldType == typeof(string))
            {
                result.Add((string)field.GetValue(null)!);
            }
        }

        foreach (var nested in type.GetNestedTypes())
        {
            Collect(nested, result);
        }
    }
}
