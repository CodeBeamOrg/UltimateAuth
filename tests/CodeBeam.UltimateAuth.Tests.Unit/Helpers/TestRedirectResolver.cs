using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestRedirectResolver : IAuthRedirectResolver
{
    private readonly AuthRedirectResolver _inner;

    private TestRedirectResolver(AuthRedirectResolver inner)
    {
        _inner = inner;
    }

    public RedirectDecision ResolveSuccess(AuthFlowContext flow, HttpContext ctx)
        => _inner.ResolveSuccess(flow, ctx);

    public RedirectDecision ResolveFailure(AuthFlowContext flow, HttpContext ctx, AuthFailureReason reason, LoginResult? result = null)
        => _inner.ResolveFailure(flow, ctx, reason, result);

    public static TestRedirectResolver Create(IEnumerable<IClientBaseAddressProvider>? providers = null)
    {
        var baseProviders = providers?.ToList() ?? new List<IClientBaseAddressProvider>
        {
            new OriginHeaderBaseAddressProvider(),
            new RefererHeaderBaseAddressProvider(),
            new ConfiguredClientBaseAddressProvider(),
            new RequestHostBaseAddressProvider()
        };

        var baseResolver = new ClientBaseAddressResolver(baseProviders);
        var authResolver = new AuthRedirectResolver(baseResolver);

        return new TestRedirectResolver(authResolver);
    }
}
