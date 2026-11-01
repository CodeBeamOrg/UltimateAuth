---
title: Rate Limiting
order: 8
group: security
---

# Rate Limiting

UltimateAuth provides built-in HTTP endpoint rate limiting using ASP.NET Core's native rate limiting infrastructure, together with client-side handling of HTTP 429 responses.

Rate limiting helps reduce excessive authentication requests, credential attacks, recovery abuse, and unnecessary session traffic without introducing a separate persistence dependency.

The server enforces request limits. UltimateAuth clients recognize throttled responses, preserve valid authentication state, and defer selected background operations when appropriate.

No additional rate limiting package is required.

## Getting started

Rate limiting is enabled by default when using the integrated ASP.NET Core middleware helper.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.RateLimiting.Standard.PermitLimit = 240;
    options.RateLimiting.Login.PermitLimit = 40;

    options.RateLimiting.Endpoints[UAuthEndpoint.Refresh] = new()
    {
        PermitLimit = 1200,
        Window = TimeSpan.FromMinutes(1)
    };
});
```

Configure the middleware pipeline:

```csharp
app.UseForwardedHeaders();
app.UseRouting();

app.UseUltimateAuthWithAspNetCore();

app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapUAuthHub();
```

Import `CodeBeam.UltimateAuth.Server.RateLimiting` for rate limiting rules and endpoint identifiers.

Configure forwarded headers only when appropriate for the deployment, using explicitly trusted proxies and networks.

Rate limiting options are also available through the `UltimateAuth:Server:RateLimiting` configuration section. The existing server options binding behavior applies, including configuration-file precedence.

Rate limiting settings are configured at startup. They are not automatically scoped to individual clients or authentication modes.

## Middleware integration

The integrated helper exposes a single method with two optional parameters:

```csharp
public static IApplicationBuilder UseUltimateAuthWithAspNetCore(
    this IApplicationBuilder app,
    bool? enableCors = null,
    bool useRateLimiting = true)
```

The default middleware order is:

1. CORS, when enabled or required by UAuthHub
2. UltimateAuth context middleware
3. ASP.NET Core authentication
4. ASP.NET Core rate limiting
5. ASP.NET Core authorization

This places the rate limiter after authentication, allowing authenticated endpoint policies to use validated UltimateAuth actor information.

### Configuration examples

```csharp
// Automatic CORS selection; rate limiting enabled
app.UseUltimateAuthWithAspNetCore();

// Explicit CORS; rate limiting enabled
app.UseUltimateAuthWithAspNetCore(enableCors: true);

// Automatic CORS; rate limiting disabled
app.UseUltimateAuthWithAspNetCore(useRateLimiting: false);

// Explicit configuration of both options
app.UseUltimateAuthWithAspNetCore(enableCors: true, useRateLimiting: true);
```

`enableCors = null` automatically enables CORS when required by UAuthHub. Setting it to `false` does not remove the host's responsibility to configure CORS when needed.

The standalone UltimateAuth middleware helper does not install ASP.NET Core authentication. If authentication has not already established a validated UltimateAuth actor, rate limiting partitioning falls back to the connection IP.

### Existing application rate limiters

If the application already installs its own `UseRateLimiter()` middleware, disable the helper-owned installation:

```csharp
app.UseRouting();
app.UseAuthentication();

app.UseUltimateAuthWithAspNetCore(useRateLimiting: false);

app.UseRateLimiter();
```

This illustrates middleware ownership rather than a universal pipeline layout. Hosts must preserve the required routing, authentication, CORS, and authorization order and avoid duplicate middleware registration.

UltimateAuth cannot detect a separately installed native rate limiting middleware instance.

Repeated calls to the UltimateAuth helper install at most one helper-owned rate limiter on the same application builder. Other middleware installed by the helper is not deduplicated.

An application builder created with `ApplicationBuilder.New()` has its own middleware installation lifecycle.

Disabling UltimateAuth's built-in endpoint policies does not disable an application-owned global limiter or native application policies.

## Rules and default limits

UltimateAuth uses native sliding-window limiters with six segments and no request queue.

The default rule groups are:

| Rule | Default | Typical operations |
|---|---|---|
| Sensitive | 30 requests/minute | Sensitive credential and recovery operations |
| Standard | 120 requests/minute | Authenticated management operations |
| Frequent | 600 requests/minute | Validation, refresh, logout, authorization checks |
| Login | 20 requests/minute/IP | Shared credential-verification budget |

These are starting defaults, not recommended universal production limits. Adjust them based on application traffic, expected concurrency, and deployment conditions.

Group rules act as templates for separate endpoint budgets. For example, validation and refresh do not share a counter merely because both use the Frequent group.

### Shared login budget

The Login rule is shared by:

- Login
- TryLogin
- PkceComplete
- PkceTryComplete

These credential-verification operations share the same budget for a partition.

Configure the shared limit through:

```csharp
options.RateLimiting.Login.PermitLimit = 40;
```

An explicit endpoint override replaces that endpoint's rule and separates it from the shared login budget.

### Additional sensitive defaults

Certain operations have stricter defaults than the Sensitive group:

| Operation | Default |
|---|---|
| Anonymous registration | 10 requests/10 minutes/IP |
| Anonymous credential reset initiation | 5 requests/10 minutes/IP |
| Anonymous credential reset completion | 10 requests/10 minutes/IP |
| Self identifier verification | 10 requests/10 minutes/IP |

These numeric defaults take precedence over the Sensitive group's numeric settings.

For example, increasing `Sensitive.PermitLimit` does not automatically increase the anonymous reset initiation limit.

Use an explicit endpoint override to change these limits.

## Per-endpoint configuration

Individual endpoint rules can be configured using `RateLimiting.Endpoints`.

For example, limit session validation to two requests per minute:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.RateLimiting.Endpoints[UAuthEndpoint.Validate] = new()
    {
        PermitLimit = 2,
        Window = TimeSpan.FromMinutes(1)
    };
});
```

The third request within the applicable sliding window receives HTTP 429 when the budget is exhausted.

This example is useful for testing client behavior, but two validations per minute may be too restrictive for a real application with automatic session validation.

Endpoint overrides are complete replacement rules, not partial changes merged with group numeric settings.

### Policy precedence

UltimateAuth resolves endpoint policies in the following order:

1. Native application endpoint/group rate limiting metadata, including native disable metadata
2. Explicit `Endpoints[id]` replacement
3. Shared Login rule for credential-verification operations
4. Special registration, reset, and verification defaults
5. Endpoint group template

An explicit endpoint override can replace a built-in rule, select an application-owned named policy, or disable the built-in policy for that endpoint.

All UltimateAuth endpoint mappings have explicit classifications. Endpoint registration fails when a newly mapped endpoint is missing its required classification.

## Disabling rate limiting

To disable UltimateAuth's built-in endpoint policies globally:

```csharp
options.RateLimiting.Enabled = false;
```

To disable an individual built-in rule:

```csharp
options.RateLimiting.Endpoints[UAuthEndpoint.Validate] = new()
{
    Enabled = false
};
```

A disabled UltimateAuth rule does not add native `DisableRateLimiting` metadata.

Application-owned policies and global limiters may still apply.

To prevent the integrated helper from installing the native middleware:

```csharp
app.UseUltimateAuthWithAspNetCore(useRateLimiting: false);
```

These controls have different responsibilities:

- `RateLimiting.Enabled` controls UltimateAuth's built-in policies.
- Individual rule `Enabled` settings control specific built-in rules.
- `useRateLimiting` controls whether the integrated helper installs the middleware.

## Using application-owned policies

Applications can use their own native ASP.NET Core policies instead of an UltimateAuth built-in rule.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string, MyRateLimiterPolicy>("my-auth-policy");
});

builder.Services.AddUltimateAuthServer(options =>
{
    options.RateLimiting.Endpoints[UAuthEndpoint.UserCreateAnonymous] = new()
    {
        PolicyName = "my-auth-policy"
    };
});
```

Named policies must be registered with the native rate limiting options used by the middleware.

UltimateAuth's built-in policies are inline native policies and do not reserve application policy names.

Endpoint policies do not automatically compose. Native ASP.NET Core endpoint/group precedence applies.

Native `DisableRateLimiting` metadata also follows ASP.NET Core semantics and can bypass an application global limiter.

### Rejection callbacks

Built-in UltimateAuth inline policies produce their own generic HTTP 429 response.

They do not invoke the application's middleware-level `OnRejected` callback.

Application-owned named policies can use native rejection behavior. When a named policy has no policy-level rejection callback, the effective native middleware `OnRejected` callback applies.

If an application needs complete ownership of rejection responses for a particular endpoint, use an application-owned policy.

UltimateAuth does not replace application global limiters or application rejection settings.

## HTTP 429 responses

When a built-in UltimateAuth policy rejects a request, the server returns:

- HTTP status `429 Too Many Requests`
- Problem Details content
- Stable detail code `rate_limited`
- `Cache-Control: no-store`
- `Retry-After` when the native limiter supplies retry guidance

A representative response is:

```json
{
  "title": "Too Many Requests",
  "status": 429,
  "detail": "rate_limited"
}
```

The exact Problem Details payload may include additional fields.

`Retry-After` is optional. Applications must not assume that every HTTP 429 response contains it.

Rejected requests do not execute their endpoint handlers. Consequently, a request rejected at this layer does not consume a reset or PKCE proof, rotate refresh credentials, clear logout cookies, or increment login failure counts through the rejected handler.

Authentication and other middleware executing before the rate limiter may still perform work.

### User-facing messages

`rate_limited` is a stable machine-readable error code, not a localized user-facing message.

Applications can map it to a message such as:

> Too many requests. Please wait a moment before trying again.

If a reliable retry delay is available, the application may display it.

Automatic background operations should generally handle throttling silently rather than repeatedly notifying the user.

## Client HTTP 429 handling

UltimateAuth clients recognize HTTP 429 as a temporary request throttle.

A 429 response does not establish that:

- The user's credentials are invalid
- The session has been revoked
- A token has expired
- The user is unauthorized
- Reauthentication is required

**A throttled request must not, by itself, clear a valid authenticated session.**

UltimateAuth uses its existing client result and error contracts rather than introducing a separate rate limiting exception hierarchy.

### Client result contracts

The following contracts carry throttling information where applicable:

- `UAuthTransportException`
- `TryLoginResult`
- `TryPkceLoginResult`
- `RefreshResult`
- `UAuthResult`
- `UAuthProblem`

The stable error code is `rate_limited`.

Depending on the contract, it is exposed through `Code`, `ErrorCode`, or `Problem.Detail`.

HTTP status remains 429.

`UAuthResult.Http.IsTooManyRequests` distinguishes throttling from unauthorized and forbidden results.

`RetryAfter` provides an optional `TimeSpan` where supported.

Transport DTOs preserve the raw `Retry-After` header across the JavaScript bridge.

The existing `IUAuthTryResult` interface provides default metadata properties so existing implementations are not required to add new members.

The public `IFlowClient` method signatures remain unchanged.

## Client behavior by operation

| Operation | HTTP 429 behavior |
|---|---|
| Validation | Throws a transport exception with status 429 and throttling metadata |
| Blazor authentication state | Preserves the current identity, claims, and last successful validation time |
| Refresh | Returns an unsuccessful result with status 429 and `Outcome = None` |
| Automatic refresh | Defers subsequent attempts without triggering reauthentication |
| Login preview | Returns a distinguishable throttled result rather than invalid credentials |
| PKCE preview | Returns a distinguishable throttled result |
| Try-and-commit | Does not submit the final commit after a throttled preview |
| Logout | Does not report successful logout or navigate on a confirmed 429 |
| Authorization | Returns a throttled failure, not a Forbidden result |
| Profile, user, credential, and recovery operations | Return unsuccessful results without publishing success events |
| Resource requests | Preserve HTTP 429 information without changing authentication state |

### Validation

`ValidateAsync` throws `UAuthTransportException` for HTTP 429 before attempting to deserialize a successful authentication result.

Blazor's `UAuthStateManager.EnsureAsync` recognizes this condition and preserves the current authentication snapshot.

It does not:

- Clear the current identity
- Remove claims
- Treat the user as anonymous
- Mark the session freshly validated
- Update the last successful validation timestamp

Subsequent validation attempts, including forced validation calls, are deferred during the cooldown.

A genuine invalid, expired, or revoked validation result continues to follow the normal state-clearing behavior.

### Refresh

A throttled refresh returns an unsuccessful `RefreshResult` with:

```text
StatusCode: 429
ErrorCode: rate_limited
Outcome: None
```

Misleading authentication-outcome headers are ignored on a throttled response.

Repeated refresh calls during the cooldown return the throttled result without issuing another HTTP request.

Automatic refresh scheduling respects both the configured refresh interval and the throttling deadline.

HTTP 429 does not cause reauthentication events, logout, or automatic navigation.

A genuine refresh HTTP 401 can still produce `ReauthRequired`.

### Login and PKCE

Login and PKCE preview operations expose throttling distinctly from invalid credentials.

When a TryAndCommit preview is throttled, the client does not submit the final commit form.

However, final browser-navigation form submissions retain their existing navigation semantics.

For `LoginAsync`, DirectCommit, `CompletePkceLoginAsync`, and the final TryAndCommit submission, the browser owns the resulting HTTP response.

If that final POST receives HTTP 429, the browser may display the server response page.

The JavaScript client cannot reliably return the final navigated response's `Retry-After` metadata through the existing navigation contract.

Preview success or form submission completion must not be interpreted as confirmation that a session was issued.

### Logout

Logout uses an observed form POST so the client can distinguish a confirmed successful response from HTTP 429.

On HTTP 429:

- No successful logout event is published.
- No success navigation occurs.
- The client does not clear authentication state solely because of throttling.

On confirmed success, the existing logout event can update client authentication state.

Logout navigation is different from login navigation: the observed request can follow a successful server redirect and then navigate without replaying the logout POST.

Cross-origin logout still requires compatible browser credentials, CORS configuration, and redirect behavior.

A network or redirect failure may leave the server-side logout outcome uncertain. The SDK reports an error rather than claiming success or automatically replaying the operation.

Applications can explicitly revalidate when connectivity permits.

### Custom client transports

`IUAuthRequestClient` supports an observed form request overload:

```csharp
SendFormAsync(endpoint, form, navigateOnSuccess, ct)
```

The overload has a default implementation that delegates to the original form request method.

Existing custom transport implementations therefore retain their original interface requirements.

However, the default implementation does not provide navigation behavior automatically.

Custom adapters that support logout navigation should implement the observed overload and preserve HTTP status and `Retry-After` metadata.

## Retry-After and cooldown behavior

UltimateAuth clients support standard `Retry-After` values expressed as:

- Delta seconds
- HTTP-date values

Parsing uses invariant culture and UTC.

Missing, malformed, or inaccessible values remain null in application results. Past dates produce a zero retry delay.

For internal maintenance scheduling, the SDK uses:

- A conservative 30-second fallback when retry guidance is unavailable
- A one-second minimum to avoid immediate retry loops
- Bounded timer intervals when waiting for long delays

Long retry guidance is honored by the current implementation.

The SDK does not automatically retry arbitrary application operations.

In particular, it does not automatically replay login, logout, recovery, credential mutations, or administrative changes after throttling.

For cross-origin browser clients, the server must expose `Retry-After` through CORS if JavaScript is expected to read that header.

When the header is not accessible, the client follows its missing-header behavior.

## Authentication state safety

Rate limiting is deliberately separated from authentication validity.

A throttled request does not:

- Extend token expiration
- Renew credentials
- Update successful validation timestamps
- Create a trusted identity
- Revoke an existing session
- Clear browser storage automatically
- Publish successful mutation events

`UAuthStateManager` remains responsible for authentication state transitions.

`SessionCoordinator` remains responsible for automatic refresh scheduling and genuine reauthentication handling.

`UAuthAuthenticationStateProvider` projects the retained state to Blazor.

Explicit local actions such as `Clear` and `OnLogoutAsync`, and authoritative revocation or mutation events, retain their existing behavior.

## Partitioning and trust boundaries

UltimateAuth chooses rate limiting partitions using trusted server-side information.

Credential entry, anonymous registration and recovery, identifier availability, PKCE, Hub entry, and self-proof verification use the connection IP even when a cookie is present.

Other operations use validated UltimateAuth tenant and actor information when available, otherwise the connection IP.

The following do not independently establish a trusted authenticated partition:

- Mere presence of a session cookie
- Usernames or target user identifiers
- Unvalidated tokens
- Device identifiers
- Client profile hints
- Raw tenant selectors
- Principals from unrelated authentication authorities

Authenticated actor partitions include validated tenant identity.

Anonymous IP budgets span tenants, preventing a client from multiplying its quota simply by rotating tenant selectors.

IPv4-mapped IPv6 addresses are normalized to IPv4.

Requests without an available connection address share an unknown-address partition.

UltimateAuth does not trust arbitrary forwarding headers.

Configure trusted proxies, forwarded headers, and ingress restrictions at the application or infrastructure level.

Applications using another identity authority should configure an appropriate native policy rather than assuming UltimateAuth can validate that authority's principal.

## Persistence and distributed deployment

Built-in counters are maintained in application memory.

They are not stored in UltimateAuth's persistence providers, EF Core, or a database.

This means:

- Counters reset when the application process restarts.
- Each application instance maintains its own counters.
- Browser refreshes do not reset server counters.
- Sliding windows replenish capacity over time.
- Multiple application instances do not automatically share a global request budget.

For distributed deployments requiring fleet-wide limits, use an ingress gateway, WAF, or custom native policy backed by shared infrastructure.

Built-in native partitions have idle cleanup, but do not provide a hard partition-cardinality bound.

Large-scale IP rotation, IPv6 address churn, NAT sharing, and botnet traffic require additional operational consideration.

Rate limiting does not replace account lockout, proof-attempt limits, anti-enumeration responses, or other authentication security controls.

Direct service calls and Blazor Server circuit operations are outside HTTP endpoint throttling unless the application protects them separately.

## Testing rate limiting

A simple development test is to temporarily limit validation to two requests per minute:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.RateLimiting.Endpoints[UAuthEndpoint.Validate] = new()
    {
        PermitLimit = 2,
        Window = TimeSpan.FromMinutes(1)
    };
});
```

After starting the application and authenticating, send repeated requests to:

```http
POST /auth/validate
```

The expected server behavior is:

| Request | Expected result |
|---|---|
| First permitted request | Normal validation |
| Second permitted request | Normal validation |
| Request exceeding the budget | HTTP 429 |
| Request after capacity replenishes | Normal validation |

Actual results depend on the partition's existing traffic and sliding-window state.

When testing through an UltimateAuth client, the client may defer additional validation requests after receiving HTTP 429. This is expected and should be tested separately from server enforcement.

Also verify that:

- Authentication state remains intact after throttling.
- Other independently limited operations continue to work.
- Automatic refresh does not enter an immediate retry loop.
- Login preview does not confuse throttling with invalid credentials.
- Logout does not report success after a confirmed 429.
- No successful mutation event is published for a rejected operation.

Remove temporary low-limit overrides before deploying to production.

### Client IP Address & Reverse Proxies

UltimateAuth uses ASP.NET Core's resolved client IP address for IP-based rate limiting. It does not directly trust forwarded IP headers.

When deploying behind Caddy, Nginx, Cloudflare, or another reverse proxy, ensure that ASP.NET Core's Forwarded Headers Middleware is correctly configured with trusted proxies or networks.

Without proper configuration, multiple users may be identified by the same proxy IP address and share rate limits.

See [Client IP Address & Reverse Proxy Setup](/docs/getting-started/real-world-setup#10-client-ip-address--reverse-proxy) for configuration examples.

## Related documentation

- [Server Options](/docs/configuration/server-options)
- [Client Options](/docs/configuration/client-options)
- [Authentication Model](/docs/concepts/authentication-model)

Rate limiting is one layer of UltimateAuth's security model. It should be combined with appropriate credential policies, session validation, trusted proxy configuration, and deployment-level protection.
