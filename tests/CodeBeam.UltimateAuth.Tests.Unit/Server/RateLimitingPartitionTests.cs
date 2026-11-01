using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.Extensions.DependencyInjection;
using CodeBeam.UltimateAuth.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public class RateLimitingPartitionTests
{
    [Fact]
    public void DuplicateServerRegistrationRetainsExistingGuard()
    {
        var services = new ServiceCollection();
        services.AddUltimateAuthServer();
        var error = Assert.Throws<InvalidOperationException>(() => services.AddUltimateAuthServer());
        Assert.Contains("policies already registered", error.Message);
    }

    [Fact]
    public async Task RejectionForwardsNativeRetryMetadata()
    {
        using var limiter = new System.Threading.RateLimiting.FixedWindowRateLimiter(new()
        { PermitLimit = 1, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
        using var acquired = limiter.AttemptAcquire();
        using var rejected = limiter.AttemptAcquire();
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        var policy = new UAuthRateLimitPolicy(UAuthEndpointClassification.Get(UAuthEndpoint.Login), new(), "login");
        await policy.OnRejected!(new Microsoft.AspNetCore.RateLimiting.OnRejectedContext
        { HttpContext = context, Lease = rejected }, default);
        Assert.Equal(429, context.Response.StatusCode);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
        Assert.True(int.Parse(context.Response.Headers.RetryAfter.ToString()) > 0);
    }

    [Fact]
    public void EveryEndpointHasTheReviewedClassification()
    {
        var all = Enum.GetValues<UAuthEndpoint>().Select(UAuthEndpointClassification.Get).ToArray();
        Assert.Equal(79, all.Length);
        Assert.Equal(20, all.Count(x => x.Group == UAuthRateLimitGroup.Sensitive));
        Assert.Equal(55, all.Count(x => x.Group == UAuthRateLimitGroup.Standard));
        Assert.Equal(4, all.Count(x => x.Group == UAuthRateLimitGroup.Frequent));
        Assert.Equal(UAuthRateLimitGroup.Sensitive, UAuthEndpointClassification.Get(UAuthEndpoint.CredentialResetBeginAnonymous).Group);
        Assert.Equal(UAuthRateLimitGroup.Standard, UAuthEndpointClassification.Get(UAuthEndpoint.VerifyUserIdentifierAdmin).Group);
        Assert.Equal(UAuthRateLimitGroup.Frequent, UAuthEndpointClassification.Get(UAuthEndpoint.Logout).Group);
    }

    [Fact]
    public void ValidatedActorsAreTenantIsolated_AndTargetsCannotChangeBudget()
    {
        var policy = new UAuthRateLimitPolicy(UAuthEndpointClassification.Get(UAuthEndpoint.Refresh), new(), "refresh");
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        var anonymous = policy.GetPartition(context).PartitionKey;
        UAuthRateLimitPolicy.SetValidatedActor(context, "tenant-a", "actor");
        var actor = policy.GetPartition(context).PartitionKey;
        Assert.NotEqual(anonymous, actor);
        context.Request.RouteValues["userKey"] = "target-2";
        context.Request.Headers["X-UAuth-ClientProfile"] = "Maui";
        Assert.Equal(actor, policy.GetPartition(context).PartitionKey);
        UAuthRateLimitPolicy.SetValidatedActor(context, "tenant-b", "actor");
        Assert.NotEqual(actor, policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public void LoginAlwaysUsesIp_AndForwardingHeadersAreNotTrusted()
    {
        var policy = new UAuthRateLimitPolicy(UAuthEndpointClassification.Get(UAuthEndpoint.Login), new(), "login");
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.0.2.1");
        var key = policy.GetPartition(context).PartitionKey;
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.8";
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.9";
        UAuthRateLimitPolicy.SetValidatedActor(context, "tenant-a", "actor");
        Assert.Equal(key, policy.GetPartition(context).PartitionKey);
        Assert.Equal("192.0.2.1", CodeBeam.UltimateAuth.Server.Infrastructure.ClientIpAddress.Resolve(context));
    }

    [Fact]
    public void OptionsCloneDoesNotShareMutableRules()
    {
        var original = new UAuthRateLimitingOptions();
        Assert.Equal(30, original.Sensitive.PermitLimit);
        Assert.Equal(120, original.Standard.PermitLimit);
        Assert.Equal(600, original.Frequent.PermitLimit);
        var reset = original.Resolve(UAuthEndpointClassification.Get(UAuthEndpoint.CredentialResetBeginAnonymous));
        Assert.Equal(5, reset.Rule.PermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(10), reset.Rule.Window);
        original.Standard.PermitLimit = 240;
        Assert.Equal(240, original.Resolve(UAuthEndpointClassification.Get(UAuthEndpoint.GetMyChains)).Rule.PermitLimit);
        original.Endpoints[UAuthEndpoint.Login] = new() { PermitLimit = 8 };
        var clone = original.Clone();
        clone.Login.PermitLimit = 1;
        clone.Endpoints[UAuthEndpoint.Login].PermitLimit = 2;
        Assert.Equal(20, original.Login.PermitLimit);
        Assert.Equal(8, original.Endpoints[UAuthEndpoint.Login].PermitLimit);
        Assert.NotNull(UAuthRateLimitValidation.Validate(new() { Frequent = new() { PermitLimit = 0 } }));
    }
}
