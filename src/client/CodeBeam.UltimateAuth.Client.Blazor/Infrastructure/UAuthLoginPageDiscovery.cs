using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

/// <summary>
/// Discovers the login page route from the component decorated with
/// <see cref="UAuthLoginPageAttribute"/>.
/// </summary>
public static class UAuthLoginPageDiscovery
{
    private const string DefaultLoginRoute = "/login";

    private static string? _cached;

    /// <summary>
    /// Resolves the login page route by scanning loaded assemblies for a component
    /// decorated with <see cref="UAuthLoginPageAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Route selection order:
    /// <list type="number">
    /// <item>Preferred route explicitly configured on <see cref="UAuthLoginPageAttribute"/>.</item>
    /// <item>Root route (<c>/</c>).</item>
    /// <item>Conventional login route (<c>/login</c>).</item>
    /// <item>First route in deterministic ordinal-ignore-case order.</item>
    /// <item>Default route (<c>/login</c>) when the component has no route.</item>
    /// </list>
    /// </remarks>
    public static string Resolve()
    {
        if (_cached is not null)
            return _cached;

        var candidates = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(HasLoginPageAttribute)
            .ToArray();

        if (candidates.Length == 0)
            return _cached = DefaultLoginRoute;

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                "Multiple [UAuthLoginPage] components were found. " +
                "Make sure only one component is marked as the UltimateAuth login page.");
        }

        return _cached = ResolveRoute(candidates[0]);
    }

    internal static string ResolveRoute(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var loginPage = componentType
            .GetCustomAttributes(typeof(UAuthLoginPageAttribute), inherit: true)
            .Cast<UAuthLoginPageAttribute>()
            .SingleOrDefault();

        if (loginPage is null)
        {
            throw new InvalidOperationException(
                $"Component '{componentType.FullName}' is not decorated with [UAuthLoginPage].");
        }

        var routes = componentType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(x => x.Template)
            .ToArray();

        return ResolveRoute(
            loginPage,
            routes,
            componentType.FullName);
    }

    internal static string ResolveRoute(
        UAuthLoginPageAttribute loginPage,
        IEnumerable<string> routes,
        string? componentName = null)
    {
        ArgumentNullException.ThrowIfNull(loginPage);
        ArgumentNullException.ThrowIfNull(routes);

        var normalizedRoutes = routes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeRoute)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(loginPage.PreferredRoute))
        {
            var normalizedPreferred =
                NormalizeRoute(loginPage.PreferredRoute);

            var preferred = normalizedRoutes.FirstOrDefault(x =>
                string.Equals(
                    x,
                    normalizedPreferred,
                    StringComparison.OrdinalIgnoreCase));

            if (preferred is null)
            {
                var componentDescription =
                    string.IsNullOrWhiteSpace(componentName)
                        ? "the login page component"
                        : $"component '{componentName}'";

                throw new InvalidOperationException(
                    $"Preferred login route '{loginPage.PreferredRoute}' " +
                    $"is not defined on {componentDescription}.");
            }

            return preferred;
        }

        var root = normalizedRoutes.FirstOrDefault(x =>
            string.Equals(
                x,
                "/",
                StringComparison.OrdinalIgnoreCase));

        if (root is not null)
            return root;

        var login = normalizedRoutes.FirstOrDefault(x =>
            string.Equals(
                x,
                DefaultLoginRoute,
                StringComparison.OrdinalIgnoreCase));

        if (login is not null)
            return login;

        if (normalizedRoutes.Length > 0)
        {
            return normalizedRoutes
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        return DefaultLoginRoute;
    }

    private static IEnumerable<Type> GetLoadableTypes(
        System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(x => x is not null)
                .Cast<Type>();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static bool HasLoginPageAttribute(Type type)
    {
        return type
            .GetCustomAttributes(
                typeof(UAuthLoginPageAttribute),
                inherit: true)
            .Any();
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return "/";

        route = route.Trim();

        if (!route.StartsWith('/'))
            route = "/" + route;

        if (route.Length > 1)
            route = route.TrimEnd('/');

        return route;
    }
}