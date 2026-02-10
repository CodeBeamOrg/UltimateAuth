using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class AuthRedirectResolver : IAuthRedirectResolver
{
    private readonly ClientBaseAddressResolver _baseAddressResolver;

    public AuthRedirectResolver(ClientBaseAddressResolver baseAddressResolver)
    {
        _baseAddressResolver = baseAddressResolver;
    }

    public RedirectDecision ResolveSuccess(AuthFlowContext flow, HttpContext ctx)
        => Resolve(flow, ctx, flow.Response.Redirect.SuccessPath, null);

    public RedirectDecision ResolveFailure(AuthFlowContext flow, HttpContext ctx, AuthFailureReason reason)
        => Resolve(flow, ctx, flow.Response.Redirect.FailurePath, reason);

    private RedirectDecision Resolve(AuthFlowContext flow, HttpContext ctx, string? fallbackPath, AuthFailureReason? failureReason)
    {
        var redirect = flow.Response.Redirect;

        if (!redirect.Enabled)
            return RedirectDecision.None();

        if (redirect.AllowReturnUrlOverride && flow.ReturnUrlInfo is { } info)
        {
            if (info.IsAbsolute)
            {
                var origin = info.AbsoluteUri!.GetLeftPart(UriPartial.Authority);
                ValidateAllowed(origin, flow.OriginalOptions);
                return RedirectDecision.To(info.AbsoluteUri.ToString());
            }

            if (!string.IsNullOrWhiteSpace(info.RelativePath))
            {
                var baseAddress = _baseAddressResolver.Resolve(ctx, flow.OriginalOptions);
                return RedirectDecision.To(
                    UrlComposer.Combine(baseAddress, info.RelativePath));
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            var baseAddress = _baseAddressResolver.Resolve(ctx, flow.OriginalOptions);

            IDictionary<string, string?>? query = null;

            if (failureReason is not null)
            {
                var code = redirect.FailureCodes != null &&
                           redirect.FailureCodes.TryGetValue(failureReason.Value, out var mapped)
                    ? mapped
                    : "failed";

                query = new Dictionary<string, string?>
                {
                    [redirect.FailureQueryKey ?? "error"] = code
                };
            }

            return RedirectDecision.To(UrlComposer.Combine(baseAddress, fallbackPath, query));
        }

        return RedirectDecision.None();
    }

    private static void ValidateAllowed(string baseAddress, UAuthServerOptions options)
    {
        if (options.Hub.AllowedClientOrigins.Count == 0)
            return;

        if (!options.Hub.AllowedClientOrigins.Any(o => Normalize(o) == Normalize(baseAddress)))
        {
            throw new InvalidOperationException($"Redirect to '{baseAddress}' is not allowed.");
        }
    }

    private static string Normalize(string uri) => uri.TrimEnd('/').ToLowerInvariant();
}
