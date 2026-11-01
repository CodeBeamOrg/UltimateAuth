using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.RateLimiting;

internal readonly record struct UAuthPartitionKey(string Budget, string Identity);

internal sealed class UAuthRateLimitPolicy : IRateLimiterPolicy<UAuthPartitionKey>
{
    private static readonly object ActorKey = new();
    private readonly UAuthRateLimitMetadata _metadata;
    private readonly UAuthRateLimitRule _rule;
    private readonly string _budget;

    internal UAuthRateLimitPolicy(UAuthRateLimitMetadata metadata, UAuthRateLimitRule rule, string budget)
    {
        _metadata = metadata; _rule = rule.Clone(); _budget = budget;
    }

    internal static void SetValidatedActor(HttpContext context, string tenant, string user) =>
        context.Items[ActorKey] = (tenant, user);

    public RateLimitPartition<UAuthPartitionKey> GetPartition(HttpContext context)
    {
        var identity = !_metadata.IpOnly && context.Items.TryGetValue(ActorKey, out var actor)
            && actor is ValueTuple<string, string> validated
            ? $"user:{validated.Item1.Length}:{validated.Item1}:{validated.Item2}"
            : "ip:" + Infrastructure.ClientIpAddress.Resolve(context);
        return RateLimitPartition.GetSlidingWindowLimiter(new UAuthPartitionKey(_budget, identity), _ => new()
        {
            PermitLimit = _rule.PermitLimit,
            Window = _rule.Window,
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => RejectAsync;

    private async ValueTask RejectAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        var response = rejected.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers.CacheControl = "no-store";
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            response.Headers.RetryAfter = Math.Max(1, Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        await Results.Problem(statusCode: 429, title: "Too many requests",
            detail: "rate_limited", extensions: new Dictionary<string, object?>
            { ["traceId"] = rejected.HttpContext.TraceIdentifier }).ExecuteAsync(rejected.HttpContext);
    }
}

internal static class UAuthRateLimitEndpointExtensions
{
    internal static T WithUAuthRateLimit<T>(this T builder, UAuthEndpoint endpoint) where T : IEndpointConventionBuilder
    {
        var metadata = UAuthEndpointClassification.Get(endpoint);
        builder.Add(b => b.Metadata.Add(metadata));
        builder.Finally(b =>
        {
            // Includes policies inherited from application route groups. Native Disable also wins.
            if (b.Metadata.OfType<EnableRateLimitingAttribute>().Any() ||
                b.Metadata.OfType<DisableRateLimitingAttribute>().Any()) return;
            var options = b.ApplicationServices.GetRequiredService<IOptions<UAuthServerOptions>>().Value.RateLimiting;
            var (rule, budget) = options.Resolve(metadata);
            if (!options.Enabled || !rule.Enabled) return;
            var immediate = new ImmediateConventionBuilder(b);
            if (rule.PolicyName is not null) immediate.RequireRateLimiting(rule.PolicyName);
            else immediate.RequireRateLimiting(new UAuthRateLimitPolicy(metadata, rule, budget));
        });
        return builder;
    }

    // Uses the public native extension to build inline-policy metadata during finalization.
    private sealed class ImmediateConventionBuilder(EndpointBuilder endpoint) : IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention) => convention(endpoint);
    }
}

internal static class UAuthRateLimitValidation
{
    internal static string? Validate(UAuthRateLimitingOptions? options)
    {
        if (options is null || options.Endpoints is null) return "RateLimiting and its Endpoints cannot be null.";
        if (options.Endpoints.Keys.Any(key => !Enum.IsDefined(typeof(UAuthEndpoint), key)))
            return "RateLimiting.Endpoints contains an unknown endpoint.";
        foreach (var rule in new[] { options.Sensitive, options.Standard, options.Frequent, options.Login }.Concat(options.Endpoints.Values))
        {
            if (rule is null) return "RateLimiting rules cannot be null.";
            if (rule.PolicyName is not null)
            {
                if (string.IsNullOrWhiteSpace(rule.PolicyName)) return "RateLimiting.PolicyName cannot be blank.";
            }
            else if (rule.PermitLimit <= 0 || rule.Window.Ticks < 6)
                return "RateLimiting requires a positive PermitLimit and a window of at least six ticks.";
        }
        return null;
    }
}
