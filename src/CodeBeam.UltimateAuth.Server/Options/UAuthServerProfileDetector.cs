using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Options
{
    internal sealed class UAuthServerProfileDetector : IServerProfileDetector
    {
        public UAuthClientProfile Detect(IServiceProvider sp)
        {
            if (Type.GetType("Microsoft.Maui.Controls.Application, Microsoft.Maui.Controls",throwOnError: false) is not null)
                return UAuthClientProfile.Maui;

            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.AspNetCore.Components.WebAssembly"))
                return UAuthClientProfile.BlazorWasm;

            // Warning: This detection method may not be 100% reliable in all hosting scenarios.
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.AspNetCore.Components.Server"))
            {
                return UAuthClientProfile.BlazorServer;
            }

            if (sp.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor>() is not null)
                return UAuthClientProfile.Mvc;

            return UAuthClientProfile.NotSpecified;
        }
    }
}
