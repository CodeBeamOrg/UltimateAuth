using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public HttpContextCurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated => Snapshot?.IsAuthenticated == true;

    public UserKey UserKey => Snapshot?.UserId ?? throw new InvalidOperationException("Current user is not authenticated.");

    private AuthUserSnapshot<UserKey>? Snapshot => _http.HttpContext?.Items[UAuthConstants.HttpItems.UserContextKey] as AuthUserSnapshot<UserKey>;
}
