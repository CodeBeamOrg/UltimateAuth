using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IClientProfileReader
{
    Task<UAuthClientProfile> ReadAsync(HttpContext context);
}
