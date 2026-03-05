using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

public static class UAuthLoginPageDiscovery
{
    private static string? _cached;

    public static string Resolve()
    {
        if (_cached != null)
            return _cached;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var candidates = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t => t.GetCustomAttributes(typeof(UAuthLoginPageAttribute), true).Any())
            .ToList();

        if (candidates.Count == 0)
            return _cached = "/login";

        if (candidates.Count > 1)
            throw new InvalidOperationException("Multiple [UAuthLoginPage] found. Make sure you only have one login page that attribute defined or define Navigation.LoginResolver explicitly.");

        var routeAttr = candidates[0].GetCustomAttributes(typeof(RouteAttribute), true).FirstOrDefault() as RouteAttribute;

        _cached = routeAttr?.Template ?? "/login";
        return _cached;
    }
}
