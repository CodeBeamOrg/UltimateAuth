//using CodeBeam.UltimateAuth.Core.Domain;
//using Microsoft.AspNetCore.Http;

//namespace CodeBeam.UltimateAuth.Server.Endpoints;

//internal sealed class PkceEndpointHandlerBridge : IPkceEndpointHandler
//{
//    private readonly PkceEndpointHandler<UserKey> _inner;

//    public PkceEndpointHandlerBridge(PkceEndpointHandler<UserKey> inner)
//    {
//        _inner = inner;
//    }

//    public Task<IResult> AuthorizeAsync(HttpContext ctx) => _inner.AuthorizeAsync(ctx);

//    public Task<IResult> CompleteAsync(HttpContext ctx) => _inner.CompleteAsync(ctx);
//}
