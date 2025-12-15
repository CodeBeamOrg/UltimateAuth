using Microsoft.AspNetCore.Http;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    /// <summary>
    /// Resolves device and client metadata from the current HTTP context.
    /// </summary>
    public interface IDeviceResolver
    {
        DeviceInfo Resolve(HttpContext context);
    }
}
