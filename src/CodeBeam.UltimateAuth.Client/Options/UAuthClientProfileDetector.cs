using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Client.Options
{
    internal sealed class UAuthClientProfileDetector : IClientProfileDetector
    {
        public UAuthClientProfile Detect(IServiceProvider sp)
        {
            if (Type.GetType("Microsoft.Maui.Controls.Application, Microsoft.Maui.Controls", throwOnError: false) is not null)
                return UAuthClientProfile.Maui;

            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.AspNetCore.Components.WebAssembly"))
                return UAuthClientProfile.BlazorWasm;

            //if (sp.GetService<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>() is not null)
            //    return UAuthClientProfile.BlazorServer;

            //if (sp.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor>() is not null)
            //    return UAuthClientProfile.Mvc;

            return UAuthClientProfile.NotSpecified;
        }
    }
}
