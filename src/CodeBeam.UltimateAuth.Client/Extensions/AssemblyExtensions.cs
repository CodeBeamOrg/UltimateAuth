using System.Reflection;

namespace CodeBeam.UltimateAuth.Client;

public static class UAuthAssemblies
{
    public static Assembly[] WithUltimateAuth(this IEnumerable<Assembly>? assemblies)
    {
        var authAssembly = typeof(UAuthClientMarker).Assembly;

        if (assemblies is null)
            return new[] { authAssembly };

        return assemblies.Append(authAssembly).DistinctBy(a => a.FullName).ToArray();
    }

    public static Assembly[] Client()
    {
        return new[] { typeof(UAuthClientMarker).Assembly };
    }
}