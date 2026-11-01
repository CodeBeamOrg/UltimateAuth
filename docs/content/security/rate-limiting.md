---
title: Rate Limiting
order: 8
group: security
---

# Built-in endpoint rate limiting

UltimateAuth uses ASP.NET Core's native rate limiter. No additional limiter package is needed.

```csharp
builder.Services.AddUltimateAuthServer(o =>
{
    o.RateLimiting.Standard.PermitLimit = 240;
    o.RateLimiting.Login.PermitLimit = 40; // Shared credential-verification budget
    o.RateLimiting.Endpoints[UAuthEndpoint.Refresh] = new()
    {
        PermitLimit = 1200,
        Window = TimeSpan.FromMinutes(1)
    };
});

app.UseForwardedHeaders(); // Configure trusted proxies/networks in the host.
app.UseRouting();
app.UseUltimateAuthWithAspNetCore(); // CORS when required, UA context, authentication, rate limiter, authorization
app.UseAntiforgery();
app.MapUltimateAuthEndpoints();
app.MapUAuthHub();
```

Import `CodeBeam.UltimateAuth.Server.RateLimiting` for rule and endpoint types. Settings also bind from `UltimateAuth:Server:RateLimiting`, with the existing configuration-file precedence. These are startup settings, not per-client or per-auth-mode settings.

`UseUltimateAuth(useRateLimiting: false)` leaves middleware ownership with the application by default. Opt in explicitly if desired. It does not add ASP.NET authentication: without prior validated authentication, partitioning falls back to IP.

`UseUltimateAuthWithAspNetCore(enableCors: null, useRateLimiting: true)` installs the native middleware after authentication and before authorization by default. The original `(app)` and `(app, bool? enableCors = null)` method signatures remain available. The new overload requires both `enableCors` and `useRateLimiting`: for example, `app.UseUltimateAuthWithAspNetCore(enableCors: null, useRateLimiting: false)`. Existing positional Boolean calls still configure CORS. Repeated helper calls install at most one helper-owned limiter on the same builder. A child created with `ApplicationBuilder.New()` owns its own installation; inherited properties are not evidence that middleware exists in the child. Hosts must still choose placement so a request does not traverse two independently installed limiters. Other middleware in the helpers is not deduplicated. Place routing before these helpers, particularly with tenant routes. Forwarded headers must run earlier, using the host's explicit trusted-proxy configuration. UltimateAuth does not trust all proxies or parse forwarding headers itself.

If the application already calls `UseRateLimiter()`, pass `useRateLimiting: false` to the helpers and place the application's call after routing/authentication. UltimateAuth cannot detect independently installed native middleware. Do not install both. Global policy disable controls only UltimateAuth endpoint policies; it does not remove the native middleware or disable application limits.

## Rules and defaults

Built-in rules use native sliding windows with six segments and no queue. Counters apply per instance. Group rules are templates for separate endpoint budgets: validation, refresh and logout never share their counters. Sensitive defaults to 30/minute, Standard to 120/minute, Frequent to 600/minute.

The `Login` rule defaults to 20/minute/IP and is shared by Login, TryLogin, PkceComplete and PkceTryComplete (PKCE credential verification). It is separately configurable from Sensitive. Registration defaults to 10/10 minutes/IP; anonymous reset begin to 5/10 minutes/IP; anonymous reset complete and self identifier verification to 10/10 minutes/IP. These security defaults take precedence over group numeric values. Set `Endpoints` for explicit replacements, including these operations. Endpoint overrides are complete rules and split the endpoint out of the shared login budget. Use `Login` to change the shared budget without splitting it.

A rule's `Enabled = false` removes that built-in policy, without adding native `DisableRateLimiting` metadata. `RateLimiting.Enabled = false` removes all built-in policies. Native application policies and the application global limiter remain effective. Disabled group templates do not disable the separately configured shared Login rule.

The endpoint identity enum and exhaustive classification table live beside the policies. Every registrar endpoint must explicitly attach classification; endpoint construction fails if a newly mapped route omits it. Hub entry is classified separately. Authenticated administration generally uses Standard; credential/recovery and identifier proof operations use Sensitive; validation, refresh, ordinary logout and authorization checks use Frequent. Optional endpoints retain the same classifications when enabled.

The effective-policy precedence is:

1. Native application endpoint/group enable/disable metadata, using native precedence.
2. An explicit `Endpoints[id]` replacement, including its `PolicyName` or disabled state.
3. Shared `Login` for the four credential-verification routes; changing an endpoint rule explicitly splits it out of that shared budget.
4. Built-in registration/reset/verification defaults, with their group's Enabled/PolicyName settings.
5. The endpoint's group template.

Global `RateLimiting.Enabled = false` removes UltimateAuth's contribution before any of its rules are installed. Application global limiters remain independent. Sensitive's numeric changes do not override the shared Login rule or stricter endpoint defaults; use the corresponding rule/endpoint override explicitly.

## Application native policies

An existing application endpoint/group policy or native DisableRateLimiting takes precedence over the built-in default. Endpoint policies do not compose. Native DisableRateLimiting skips the application global limiter too, exactly as in ASP.NET Core.

```csharp
builder.Services.AddRateLimiter(limits =>
{
    limits.AddPolicy<string, MyRateLimiterPolicy>("my-auth-policy");
});
builder.Services.AddUltimateAuthServer(o =>
{
    o.RateLimiting.Endpoints[UAuthEndpoint.UserCreateAnonymous] = new()
    {
        PolicyName = "my-auth-policy"
    };
});
```

Named-policy references must exist in the options used by the native middleware; ASP.NET throws for missing names. UltimateAuth's own policies are inline native policies, so there are no reserved policy names. Application GlobalLimiter and rejection settings are never replaced. **Rejection ownership follows native endpoint-policy precedence:** built-in inline policies own their generic 429 response. They do not invoke application `OnRejected` callbacks, whether configured through DI or an explicit `app.UseRateLimiter(applicationOptions)` instance. No effective middleware options are inferred from DI. Global-limiter rejections still use the effective middleware's callback.

To use application rejection behavior for an UltimateAuth endpoint, assign an application-owned named policy using `PolicyName` or native endpoint/group metadata. With a named policy that has no policy-level rejection callback, native ASP.NET Core uses the effective middleware `OnRejected`, including an explicit `applicationOptions` instance. If the named policy supplies its own callback, native policy-level precedence applies. Use `useRateLimiting: false` when installing the native middleware yourself. This is the supported contract; built-in inline policy rejections deliberately do not inherit a middleware-level callback.

Without an application callback, rejection is generic Problem Details with detail `rate_limited`, HTTP 429, no-store and native Retry-After rounded upward when supplied. Rejected requests do not execute endpoint handlers, consume PKCE/reset proofs, rotate refresh credentials, clear logout cookies or increment login failures. Existing pre-limiter authentication/session reads still occur.

## Partition trust and deployment limits

Credential entry, anonymous recovery/registration/availability, PKCE, Hub and self proof verification use connection IP even if a cookie is present. Other operations use the tenant and actor recorded by successful UltimateAuth authentication, otherwise IP. Mere session presence, unrelated host principals, usernames, tokens, device/profile hints, target user IDs and raw tenant selectors cannot create authenticated partitions. Applications with another identity authority should supply their own native policy.

IPv4-mapped IPv6 addresses normalize to IPv4. Missing addresses share one unknown-address bucket. Device metadata uses the same connection address. Configure trusted forwarding and ingress restrictions in the host; arbitrary X-Forwarded-For or vendor headers are ignored by UltimateAuth.

Anonymous IP quotas span tenants to prevent tenant-selector rotation. Authenticated actor quotas include validated tenant identity. NATs share anonymous capacity; IPv6 address rotation and botnets can evade individual-IP limits. Native partitions have idle cleanup, not a hard cardinality bound. Use gateway/WAF limits and load testing for hostile address churn. Native counters are process-local and reset on restart; use a shared ingress layer or custom native policy for fleet-wide quotas. Keep account lockout, proof-attempt limits and anti-enumeration responses: request throttling replaces none of them. Direct service/circuit calls are outside HTTP endpoint throttling.

Moderate defaults can still interrupt shared-network onboarding, support recovery, bulk administration and aggressive polling. Tune using real traffic. Repeated server DI registration retains the existing AccessPolicyRegistry exception; this feature does not introduce multiple server registrations.

## Client compatibility

Client HTTP 429 handling is described in [Client HTTP 429](client-rate-limiting.md). Fetch-based flows preserve authentication state and expose throttling metadata; full-page credential form submissions retain browser-owned response semantics. Review those boundaries during upgrade.

## Upgrade and verification

The integrated helper now installs native middleware by default. Existing applications that call `UseRateLimiter()` must opt out of helper ownership. Installing the middleware can activate an existing application GlobalLimiter across other application routes; review this during upgrade. Client-side behavior and navigation boundaries are documented in the client compatibility guide above.

The compact compatibility project reuses the middleware contract tests across .NET 8, 9 and 10 without duplicating the fixture. Run `dotnet test tests/CodeBeam.UltimateAuth.Tests.RateLimiting.Compatibility`. Real proof/refresh/recovery tests remain in the main integration project using its existing .NET 10 host.
