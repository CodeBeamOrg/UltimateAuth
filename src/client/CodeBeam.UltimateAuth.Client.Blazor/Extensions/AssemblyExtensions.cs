using System.Reflection;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public static class UAuthAssemblies
{
    public static Assembly[] WithUltimateAuth(this IEnumerable<Assembly>? assemblies)
    {
        var authAssembly = typeof(UAuthBlazorClientMarker).Assembly;

        if (assemblies is null)
            return new[] { authAssembly };

        return assemblies.Append(authAssembly).DistinctBy(a => a.FullName).ToArray();
    }

    public static Assembly[] BlazorClient()
    {
        return new[] { typeof(UAuthBlazorClientMarker).Assembly };
    }
}