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
    {
        var redirect = flow.Response.Redirect;

        if (!redirect.Enabled)
            return RedirectDecision.None();

        var baseAddress = _baseAddressResolver.Resolve(ctx, flow.OriginalOptions, flow.ReturnUrl);

        if (redirect.AllowReturnUrlOverride && !string.IsNullOrWhiteSpace(flow.ReturnUrl))
        {
            return RedirectDecision.To(ResolveReturnUrl(baseAddress, flow.ReturnUrl, flow.OriginalOptions));
        }

        if (string.IsNullOrWhiteSpace(redirect.SuccessPath))
            return RedirectDecision.None();

        return RedirectDecision.To(UrlComposer.Combine(baseAddress, redirect.SuccessPath));
    }

    public RedirectDecision ResolveFailure(AuthFlowContext flow, HttpContext context, AuthFailureReason reason)
    {
        var redirect = flow.Response.Redirect;

        if (!redirect.Enabled || string.IsNullOrWhiteSpace(redirect.FailurePath))
            return RedirectDecision.None();

        var baseAddress = _baseAddressResolver.Resolve(context, flow.OriginalOptions, flow.ReturnUrl);

        var code = redirect.FailureCodes != null && redirect.FailureCodes.TryGetValue(reason, out var mapped)
            ? mapped
            : "failed";

        var query = new Dictionary<string, string?>
        {
            [redirect.FailureQueryKey ?? "error"] = code
        };

        var url = UrlComposer.Combine(baseAddress, redirect.FailurePath, query);

        return RedirectDecision.To(url);
    }

    private static string ResolveReturnUrl(string baseAddress, string returnUrl, UAuthServerOptions options)
    {
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var abs))
        {
            var origin = abs.GetLeftPart(UriPartial.Authority);

            if (options.Hub.AllowedClientOrigins.Count > 0 && !options.Hub.AllowedClientOrigins.Any(o => Normalize(o) == Normalize(origin)))
            {
                throw new InvalidOperationException($"Redirect to '{origin}' is not allowed.");
            }

            return abs.ToString();
        }

        return UrlComposer.Combine(baseAddress, returnUrl);
    }

    private static string Normalize(string uri) => uri.TrimEnd('/').ToLowerInvariant();
}
