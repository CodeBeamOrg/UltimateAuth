using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public class DefaultLoginEndpointHandler : ILoginEndpointHandler
    {
        public Task<IResult> LoginAsync(HttpContext ctx)
            => Task.FromResult(Results.NotImplemented());
    }
}
