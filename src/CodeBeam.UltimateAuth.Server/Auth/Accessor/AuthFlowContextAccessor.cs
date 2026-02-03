using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AuthFlowContextAccessor : IAuthFlowContextAccessor
{
    private static readonly object Key = new();

    private readonly IHttpContextAccessor _http;

    public AuthFlowContextAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public AuthFlowContext Current
    {
        get
        {
            var ctx = _http.HttpContext
                ?? throw new InvalidOperationException("No HttpContext.");

            if (!ctx.Items.TryGetValue(Key, out var value) || value is not AuthFlowContext flow)
                throw new InvalidOperationException("AuthFlowContext is not available for this request.");

            return flow;
        }
    }

    internal void Set(AuthFlowContext context)
    {
        var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext.");
        ctx.Items[Key] = context;
    }
}
