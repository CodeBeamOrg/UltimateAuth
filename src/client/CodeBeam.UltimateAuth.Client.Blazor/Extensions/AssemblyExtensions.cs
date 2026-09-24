using System.Reflection;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Provides extension methods for working with assemblies in the context of UltimateAuth Blazor client applications.
/// </summary>
public static class UAuthAssemblies
{
    /// <summary>
    /// Appends the assembly containing the UAuthBlazorClientMarker class to the provided collection of assemblies,
    /// ensuring that it is included for UltimateAuth Blazor client applications.
    /// </summary>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static Assembly[] WithUltimateAuth(this IEnumerable<Assembly>? assemblies)
    {
        var authAssembly = typeof(UAuthBlazorClientMarker).Assembly;

        if (assemblies is null)
            return new[] { authAssembly };

        return assemblies.Append(authAssembly).DistinctBy(a => a.FullName).ToArray();
    }

    /// <summary>
    /// Returns an array containing the assembly of the UAuthBlazorClientMarker class,
    /// which is used to identify the UltimateAuth Blazor client application assembly.
    /// </summary>
    /// <returns></returns>
    public static Assembly[] BlazorClient()
    {
        return new[] { typeof(UAuthBlazorClientMarker).Assembly };
    }
}