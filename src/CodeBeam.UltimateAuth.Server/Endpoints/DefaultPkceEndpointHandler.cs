using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public class DefaultPkceEndpointHandler : IPkceEndpointHandler
    {
        public Task<IResult> CreateAsync(HttpContext ctx)
            =>  Task.FromResult<IResult>(Results.StatusCode(StatusCodes.Status501NotImplemented));

        public Task<IResult> VerifyAsync(HttpContext ctx)
            => Task.FromResult<IResult>(Results.StatusCode(StatusCodes.Status501NotImplemented));

        public Task<IResult> ConsumeAsync(HttpContext ctx)
            => Task.FromResult<IResult>(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
