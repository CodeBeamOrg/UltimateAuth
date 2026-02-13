using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class UAuthRazorExtensions
{
    public static RazorComponentsEndpointConventionBuilder AddUltimateAuthClientRoutes(this RazorComponentsEndpointConventionBuilder builder,Assembly clientAssembly)
    {
        return builder.AddAdditionalAssemblies(clientAssembly);
    }
}
