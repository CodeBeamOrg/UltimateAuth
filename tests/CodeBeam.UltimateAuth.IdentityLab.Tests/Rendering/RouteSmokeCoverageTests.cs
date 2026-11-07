using System.Reflection;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Rendering;

/// <summary>Connects a page to the executable render test that exercises it.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class SmokeTestForAttribute(Type componentType) : Attribute
{
    public Type ComponentType { get; } = componentType;
}

[Trait("Category", "Render")]
public sealed class RouteSmokeCoverageTests
{
    [Fact]
    public void Every_identity_lab_route_component_has_an_executable_smoke_test()
    {
        // Inspect the application assembly only. External/shared components and the
        // test assembly cannot become route candidates. A multi-route page is one
        // component; include all its aliases in any failure message.
        var pages = typeof(LoginPage).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type))
            .Select(type => new
            {
                Type = type,
                Routes = type.GetCustomAttributes<RouteAttribute>(inherit: false)
                    .Select(route => route.Template).OrderBy(route => route).ToArray()
            })
            .Where(page => page.Routes.Length > 0)
            .ToDictionary(page => page.Type, page => page.Routes);
        Assert.NotEmpty(pages);

        var mappings = typeof(PageRenderTests).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => method.GetCustomAttributes<SmokeTestForAttribute>()
                .Select(attribute => (Method: method, Page: attribute.ComponentType)))
            .ToArray();
        Assert.NotEmpty(mappings);
        foreach (var mapping in mappings)
        {
            var fact = mapping.Method.GetCustomAttribute<FactAttribute>(); // Includes TheoryAttribute.
            Assert.True(fact is not null && fact.Skip is null,
                $"{mapping.Method.DeclaringType?.Name}.{mapping.Method.Name} must be an enabled xUnit render test.");
            Assert.True(pages.ContainsKey(mapping.Page),
                $"Smoke mapping references a non-routable Identity Lab component: {mapping.Page.FullName}.");
        }

        var tested = mappings.Select(mapping => mapping.Page).ToHashSet();
        var missing = pages.Where(page => !tested.Contains(page.Key))
            .OrderBy(page => page.Key.FullName)
            .Select(page => $"{page.Key.FullName}: {string.Join(", ", page.Value)}")
            .ToArray();
        Assert.True(missing.Length == 0,
            "Identity Lab pages missing render smoke tests:\n" + string.Join("\n", missing) +
            "\nAdd a real render test and annotate it with [SmokeTestFor(typeof(PageComponent))].");
    }
}
