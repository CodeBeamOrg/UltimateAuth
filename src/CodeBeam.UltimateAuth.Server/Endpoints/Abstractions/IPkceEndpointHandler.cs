using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IPkceEndpointHandler
    {
        /// <summary>
        /// Starts the PKCE authorization flow.
        /// Creates and stores a PKCE authorization artifact
        /// and returns an authorization code or redirect instruction.
        /// </summary>
        Task<IResult> AuthorizeAsync(HttpContext ctx);

        /// <summary>
        /// Completes the PKCE flow.
        /// Atomically validates and consumes the authorization code,
        /// then issues a session or token.
        /// </summary>
        Task<IResult> CompleteAsync(HttpContext ctx);
    }
}
