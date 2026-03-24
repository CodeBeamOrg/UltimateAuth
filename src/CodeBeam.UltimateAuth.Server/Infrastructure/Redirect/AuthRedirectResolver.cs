using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class AuthRedirectResolver : IAuthRedirectResolver
{
    private readonly ClientBaseAddressResolver _baseAddressResolver;

    public AuthRedirectResolver(ClientBaseAddressResolver baseAddressResolver)
    {
        _baseAddressResolver = baseAddressResolver;
    }

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public RedirectDecision ResolveSuccess(AuthFlowContext flow, HttpContext ctx)
        => Resolve(flow, ctx, flow.Response.Redirect.SuccessPath, null);

    public RedirectDecision ResolveFailure(AuthFlowContext flow, HttpContext ctx, AuthFailureReason reason, LoginResult? loginResult = null)
        => Resolve(flow, ctx, flow.Response.Redirect.FailurePath, reason, loginResult);

    private RedirectDecision Resolve(AuthFlowContext flow, HttpContext ctx, string? fallbackPath, AuthFailureReason? failureReason, LoginResult? loginResult = null)
    {
        var redirect = flow.Response.Redirect;

        if (!redirect.Enabled)
            return RedirectDecision.None();

        if (failureReason is null && redirect.AllowReturnUrlOverride && flow.ReturnUrlInfo is { } info)
        {
            if (info.IsAbsolute && (info.AbsoluteUri!.Scheme == Uri.UriSchemeHttp || info.AbsoluteUri!.Scheme == Uri.UriSchemeHttps))
            {
                var origin = info.AbsoluteUri!.GetLeftPart(UriPartial.Authority);
                ValidateAllowed(origin, flow.OriginalOptions);
                return RedirectDecision.To(info.AbsoluteUri.ToString());
            }

            if (!string.IsNullOrWhiteSpace(info.RelativePath))
            {
                var baseAddress = _baseAddressResolver.Resolve(ctx, flow.OriginalOptions);
                return RedirectDecision.To(UrlComposer.Combine(baseAddress, info.RelativePath));
            }
        }

        if (string.IsNullOrWhiteSpace(fallbackPath))
            return RedirectDecision.None();

        var baseUrl = _baseAddressResolver.Resolve(ctx, flow.OriginalOptions);

        var query = new Dictionary<string, string?>();

        if (!string.IsNullOrWhiteSpace(flow.ReturnUrlInfo?.RelativePath))
            query["returnUrl"] = flow.ReturnUrlInfo.RelativePath;

        // Failure payload
        if (failureReason is not null)
        {
            var payload = new AuthFlowPayload
            {
                V = 1,
                Flow = flow.FlowType,
                Status = "failed",
                Reason = failureReason
            };

            if (flow.FlowType == AuthFlowType.Login && loginResult is not null && flow.OriginalOptions.Login.IncludeFailureDetails)
            {
                payload = payload with
                {
                    LockoutUntil = loginResult.LockoutUntilUtc?.ToUnixTimeSeconds(),
                    RemainingAttempts = loginResult.RemainingAttempts
                };
            }

            var json = JsonSerializer.Serialize(payload, PayloadJsonOptions);

            var encoded = Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(json));

            query["uauth"] = encoded;
        }

        return RedirectDecision.To(UrlComposer.Combine(baseUrl, fallbackPath, query));
    }

    private static void ValidateAllowed(string baseAddress, UAuthServerOptions options)
    {
        if (options.Hub.AllowedClientOrigins.Count == 0)
            return;

        var normalized = OriginHelper.Normalize(baseAddress);

        if (!options.Hub.AllowedClientOrigins.Any(o => OriginHelper.Normalize(o) == normalized))
        {
            throw new InvalidOperationException($"Redirect to '{baseAddress}' is not allowed.");
        }
    }
}
