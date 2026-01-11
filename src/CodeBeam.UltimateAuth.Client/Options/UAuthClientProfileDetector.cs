using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Client.Options
{
    internal sealed class UAuthClientProfileDetector : IClientProfileDetector
    {
        public UAuthClientProfile Detect(IServiceProvider sp)
        {
            if (sp.GetService<IUAuthHubMarker>() != null)
                return UAuthClientProfile.UAuthHub;

            if (Type.GetType("Microsoft.Maui.Controls.Application, Microsoft.Maui.Controls", throwOnError: false) is not null)
                return UAuthClientProfile.Maui;

            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.AspNetCore.Components.WebAssembly"))
                return UAuthClientProfile.BlazorWasm;

            // Warning: This detection method may not be 100% reliable in all hosting scenarios.
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.AspNetCore.Components.Server"))
            {
                return UAuthClientProfile.BlazorServer;
            }

            // Default to WebServer profile for other ASP.NET Core scenarios such as MVC, Razor Pages, minimal APIs, etc.
            // NotSpecified should only be used when user explicitly sets it. (For example in unit tests)
            return UAuthClientProfile.WebServer;
        }
    }
}
