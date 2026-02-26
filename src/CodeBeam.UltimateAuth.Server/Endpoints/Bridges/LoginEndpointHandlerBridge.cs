//using CodeBeam.UltimateAuth.Core.Domain;
//using Microsoft.AspNetCore.Http;

//namespace CodeBeam.UltimateAuth.Server.Endpoints;

//internal sealed class LoginEndpointHandlerBridge : ILoginEndpointHandler
//{
//    private readonly LoginEndpointHandler<UserKey> _inner;

//    public LoginEndpointHandlerBridge(LoginEndpointHandler<UserKey> inner)
//    {
//        _inner = inner;
//    }

//    public Task<IResult> LoginAsync(HttpContext ctx) => _inner.LoginAsync(ctx);
//}
