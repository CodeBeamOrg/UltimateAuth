---
title: Advanced Configuration
order: 4
group: configuration
---

# Advanced Configuration

UltimateAuth supports advanced customization of authentication behavior through effective server options, authentication mode resolution, dependency injection, custom infrastructure, and lifecycle events.

These mechanisms serve different purposes:

- **Options** define configurable behavior.
- **Effective options** adapt that behavior to the selected authentication mode.
- **Resolvers** determine information required by authentication flows.
- **Authorities and validators** participate in security decisions.
- **Stores** persist authentication and application state.
- **Events** allow applications to react to completed operations.

Most applications should begin with the standard Server and Client options. Advanced customization becomes useful when an application needs specialized authentication behavior, custom infrastructure, or integration with existing systems.

This guide explains the supported extension mechanisms, their execution order, and the security boundaries developers must preserve.

## 1. Configuration Versus Extensibility

UltimateAuth distinguishes between configuring existing behavior and replacing an implementation.

For example, changing a session lifetime is configuration:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromHours(8);
});
```

Providing a custom clock is an infrastructure extension:

```csharp
builder.Services.AddSingleton<IClock, ApplicationClock>();
```

`ApplicationClock` is an implementation supplied by your application.

These approaches are not interchangeable.

| Requirement | Recommended mechanism |
|---|---|
| Change session lifetime | Server options |
| Change token lifetime for a specific mode | `ConfigureMode()` |
| Select authentication mode differently | `IEffectiveAuthModeResolver` |
| Resolve tenants from a custom source | `ITenantIdResolver` |
| Customize login decisions | `ILoginAuthority` |
| Supply signing keys | `IJwtSigningKeyProvider` |
| Persist sessions in another backend | Store implementation |
| React to successful login | `UAuthEvents` |
| Add application-specific access rules | Policies |

Prefer the most specific supported mechanism rather than replacing an entire authentication component.

## 2. Understanding Effective Server Options

The server does not necessarily use the original `UAuthServerOptions` instance unchanged for every authentication flow.

Instead, UltimateAuth can resolve an effective configuration based on the client profile and authentication mode.

The relevant types are:

```csharp
IEffectiveServerOptionsProvider
EffectiveUAuthServerOptions
IEffectiveAuthModeResolver
```

### Configured Options

`UAuthServerOptions` represents the application's configured server settings.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromHours(8);
    options.Token.Issuer = "MyApplication";
});
```

These settings form the base configuration.

### Effective Options

The provider produces an `EffectiveUAuthServerOptions` instance containing:

```csharp
public sealed class EffectiveUAuthServerOptions
{
    public UAuthMode Mode { get; init; }
    public UAuthServerOptions Options { get; init; } = default!;
}
```

`Mode` identifies the resolved authentication mode.

`Options` contains the effective server configuration for that mode.

The effective configuration is derived from the original settings rather than replacing the application's registered options globally.

## 3. Effective Configuration Resolution

The current `EffectiveServerOptionsProvider` follows this sequence:

```text
Configured UAuthServerOptions
             |
             v
       Resolve mode
             |
             v
     Clone base options
             |
             v
     Apply mode defaults
             |
             v
  Apply ConfigureMode override
             |
             v
 EffectiveUAuthServerOptions
```

In simplified form:

```csharp
var original = baseOptions.Value;

var mode = modeResolver.Resolve(clientProfile);

var options = original.Clone();

ConfigureDefaults.ApplyModeDefaults(mode, options);

if (original.ModeConfigurations.TryGetValue(mode, out var configure))
{
    configure(options);
}

return new EffectiveUAuthServerOptions
{
    Mode = mode,
    Options = options
};
```

This execution order is important.

**Mode defaults are applied after the original configuration is cloned.**

Consequently, certain values configured globally may be replaced by mode-specific defaults.

Explicit mode overrides are applied last.

### What Changes Between Requests?

Different client profiles can resolve to different authentication modes.

The provider therefore can produce different effective configurations for different client profiles.

However, this does not mean every setting is independently recalculated from arbitrary request data.

In the current implementation, mode selection is based on the supplied `UAuthClientProfile`.

The provider also accepts tenant context, but the default effective-options construction does not apply an additional tenant-specific configuration override.

Applications should not assume that per-tenant options customization exists automatically.

## 4. Default Authentication Mode Resolution

The default `EffectiveAuthModeResolver` maps client profiles to authentication modes.

| Client profile | Resolved mode |
|---|---|
| `BlazorServer` | `PureOpaque` |
| `WebServer` | `Hybrid` |
| `BlazorWasm` | `Hybrid` |
| `Maui` | `Hybrid` |
| `Api` | `PureJwt` |
| Other / unknown | `Hybrid` |

These mappings describe the current default resolver.

They do not imply that every resolved mode is fully supported in the current release.

In particular, the current server options validator rejects explicit activation of `SemiHybrid` and `PureJwt` through `AllowedModes`.

Applications should verify that their client profiles resolve to a supported authentication mode before deploying.

### AllowedModes Is Not a Mode Selector

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.AllowedModes =
    [
        UAuthMode.PureOpaque,
        UAuthMode.Hybrid
    ];
});
```

This restricts which modes are allowed.

It does not instruct the resolver to choose `Hybrid` for every client.

Mode selection and mode restrictions are separate responsibilities.

## 5. Configure Authentication Modes

Use the public `ConfigureMode()` extension to customize settings for a particular authentication mode.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
    });
});
```

This configuration changes the effective access token lifetime for `Hybrid`.

It does not change the effective `PureOpaque` configuration.

### Why ConfigureMode Matters

Consider this configuration:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Token.AccessTokenLifetime = TimeSpan.FromMinutes(30);
});
```

The current `Hybrid` mode defaults explicitly set:

```csharp
Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
```

Because mode defaults are applied after the base options are cloned, the effective `Hybrid` access token lifetime becomes ten minutes rather than thirty.

To override that behavior, use:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(30);
    });
});
```

The explicit mode override is applied after the mode defaults.

### Configure Multiple Modes

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
    });

    options.ConfigureMode(UAuthMode.PureOpaque, mode =>
    {
        mode.Session.IdleTimeout = TimeSpan.FromDays(3);
    });
});
```

Each override is associated with its specified mode.

### Important Limitations

`ConfigureMode()` configures the effective options for a mode.

It does not:

- Enable an unsupported authentication mode.
- Bypass the server's allowed-mode restrictions.
- Replace the authentication mode resolver.
- Automatically create a tenant-specific configuration.
- Guarantee that every combination of effective settings is safe.

The current implementation applies mode overrides during effective-options resolution.

Do not assume that every mode-specific combination receives the same startup validation as the original `UAuthServerOptions` instance.

Test mode overrides carefully, especially when changing token issuance, session lifetimes, or credential delivery.

## 6. Mode Defaults

UltimateAuth provides mode-specific defaults for authentication behavior.

The current implementation includes templates for:

- `PureOpaque`
- `Hybrid`
- `SemiHybrid`
- `PureJwt`

These templates configure relevant token, session, cookie, and response behavior.

### PureOpaque

The `PureOpaque` defaults include:

```csharp
Token.IssueJwt = false;
Token.IssueOpaque = false;
Token.IssueRefresh = false;
```

In this mode, the session identifier is the primary opaque authentication credential.

The defaults also configure a longer idle timeout and session touch interval where no explicit values were supplied.

### Hybrid

The `Hybrid` defaults include:

```csharp
Token.IssueJwt = true;
Token.IssueOpaque = true;
Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
```

Hybrid also configures refresh-token delivery through a cookie.

### Unsupported Modes

Although default templates exist for `SemiHybrid` and `PureJwt`, their presence does not mean those modes are ready for production use.

The server's current validator explicitly rejects these modes when they are enabled through `AllowedModes`.

Only use modes supported by the current release.

## 7. Inspect Effective Options

Advanced integrations can use `IEffectiveServerOptionsProvider` to inspect configured and effective server options.

The interface exposes:

```csharp
public interface IEffectiveServerOptionsProvider
{
    UAuthServerOptions GetOriginal(HttpContext context);
    EffectiveUAuthServerOptions GetEffective(HttpContext context, UAuthClientProfile clientProfile);
    EffectiveUAuthServerOptions GetEffective(TenantKey tenant, UAuthClientProfile clientProfile);
}
```

### Inspect the Original Configuration

```csharp
var original = provider.GetOriginal(httpContext);
var configuredLifetime = original.Token.AccessTokenLifetime;
```

### Inspect the Effective Configuration

```csharp
var effective = provider.GetEffective(httpContext, UAuthClientProfile.BlazorWasm);
var mode = effective.Mode;
var lifetime = effective.Options.Token.AccessTokenLifetime;
```

This is useful when diagnosing unexpected differences between configured and runtime behavior.

For example, a globally configured access token lifetime may differ from the effective value because a mode template overrides it.

### Do Not Mutate the Original Options

`GetOriginal()` returns the original options instance.

Treat it as read-only application configuration.

Runtime customization should use the supported configuration mechanisms rather than mutating registered options during requests.

## 8. Custom Authentication Mode Resolution

UltimateAuth exposes:

```csharp
IEffectiveAuthModeResolver
```

The interface contains:

```csharp
public interface IEffectiveAuthModeResolver
{
    UAuthMode Resolve(UAuthClientProfile clientProfile);
}
```

The default implementation selects the mode using the client profile.

An application can provide its own resolver when the default mapping does not meet its requirements.

### Custom Resolver Example

```csharp
public sealed class ApplicationAuthModeResolver : IEffectiveAuthModeResolver
{
    public UAuthMode Resolve(UAuthClientProfile clientProfile)
    {
        return clientProfile switch
        {
            UAuthClientProfile.BlazorServer => UAuthMode.PureOpaque,
            UAuthClientProfile.BlazorWasm => UAuthMode.Hybrid,
            UAuthClientProfile.Maui => UAuthMode.Hybrid,

            _ => UAuthMode.Hybrid
        };
    }
}
```

Register the implementation:

```csharp
builder.Services.AddSingleton<IEffectiveAuthModeResolver, ApplicationAuthModeResolver>();
builder.Services.AddUltimateAuthServer();
```

The server registers its default resolver using `TryAddSingleton`, so an existing registration can take precedence.

### Security Considerations

The custom resolver must return a mode compatible with the server's supported modes and security requirements.

Do not choose authentication modes based solely on untrusted client-provided values without appropriate validation.

Also note that the current resolver interface receives a client profile, not an arbitrary HTTP request context.

For more complex context-dependent selection, additional application integration may be necessary.

## 9. Dependency Injection Extension Points

UltimateAuth registers many internal services through dependency injection.

Several default registrations use `TryAddScoped` or `TryAddSingleton`, allowing applications to supply an implementation before the default registration is added.

For example:

```csharp
builder.Services.AddScoped<ISessionValidator, ApplicationSessionValidator>();
builder.Services.AddUltimateAuthServer();
```

The application-defined validator is registered before the UltimateAuth server's default registration.

Because the server uses `TryAddScoped` for this service, it does not add its default implementation when an existing registration is present.

### Registration Order Matters

For services registered using `TryAdd...`, prefer:

```csharp
builder.Services.AddScoped<ISessionValidator, ApplicationSessionValidator>();
builder.Services.AddUltimateAuthServer();
```

Do not assume that registering a replacement after the server will always produce the same behavior.

The result depends on the service's registration pattern and how it is resolved.

### Not Every Service Is Replaceable in the Same Way

Some UltimateAuth services use ordinary `AddSingleton` or `AddScoped` registrations.

Others use `TryAdd...`.

Some are registered as collections.

These patterns have different override semantics.

Therefore, do not assume that every internal service can be replaced safely or that every replacement preserves the intended security behavior.

## 10. Custom Login Authority

The login authority participates in deciding whether a login attempt is permitted.

UltimateAuth exposes:

```csharp
ILoginAuthority
```

The interface is:

```csharp
public interface ILoginAuthority
{
    LoginDecision Decide(LoginDecisionContext context);
}
```

An application can implement its own login decision logic.

For example:

```csharp
public sealed class ApplicationLoginAuthority : ILoginAuthority
{
    public LoginDecision Decide(LoginDecisionContext context)
    {
        // Evaluate application-specific login rules.
        // Return a valid LoginDecision.
        throw new NotImplementedException();
    }
}
```

The example illustrates the extension contract; it is not a usable implementation.

Register the implementation:

```csharp
builder.Services.AddScoped<ILoginAuthority, ApplicationLoginAuthority>();
builder.Services.AddUltimateAuthServer();
```

### Important Security Boundary

Replacing the login authority changes a security decision component.

The replacement must preserve required authentication checks and correctly handle all decision outcomes.

Do not replace the authority merely to add logging, notifications, or metrics.

Use lifecycle events for those purposes.

If the application only needs additional access rules, consider the policy system before replacing a core authority.

## 11. Authorities, Policies, and Invariants

UltimateAuth separates different responsibilities within its security architecture.

### Authorities

Authorities evaluate security decisions.

Examples include:

```csharp
IAuthAuthority
IAccessAuthority
ILoginAuthority
```

### Policies

Policies define application-specific rules used by the authorization system.

They should be the preferred extension mechanism when the requirement is to add or refine access rules.

### Invariants

Invariants enforce foundational security requirements.

The server registers invariants including:

```csharp
DeviceRequiredInvariant
ExpiredSessionInvariant
InvalidOrRevokedSessionInvariant
TenantResolvedInvariant
```

These are registered as a collection of `IAuthorityInvariant` implementations.

Unlike ordinary application policies, security invariants protect fundamental authentication requirements.

**Do not remove or bypass invariants to make a custom authentication flow succeed.**

For example, an expired or revoked session should not become valid merely because a custom access policy grants permission.

## 12. Custom Tenant Resolution

UltimateAuth supports route, header, and host-based tenant resolution.

These mechanisms are controlled through server options.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.MultiTenant.EnableHeader = true;
});
```

For applications with a different tenant identification strategy, UltimateAuth exposes:

```csharp
ITenantIdResolver
```

The server's default registration uses `TryAddSingleton`.

A custom resolver can therefore be registered before the server:

```csharp
builder.Services.AddSingleton<ITenantIdResolver, ApplicationTenantIdResolver>();
builder.Services.AddUltimateAuthServer();
```

`ApplicationTenantIdResolver` represents an application-defined implementation.

### When to Use a Custom Resolver

A custom tenant resolver may be appropriate when tenants are identified through:

- An application-specific host naming convention.
- A trusted gateway-provided tenant identifier.
- A custom routing structure.
- An existing tenant management system.

### Security Considerations

Tenant identification is not the same as tenant authorization.

The server must still verify that the authenticated user or application is allowed to access the resolved tenant.

Never trust arbitrary tenant identifiers supplied by clients without applying the required validation and isolation rules.

## 13. Custom Session and Device Resolution

UltimateAuth separates session identifier resolution from session validation.

For example:

```csharp
ISessionIdResolver
ISessionValidator
IDeviceResolver
```

These interfaces have different responsibilities.

| Component | Responsibility |
|---|---|
| `ISessionIdResolver` | Determine which session identifier is supplied |
| `ISessionValidator` | Validate the session |
| `IDeviceResolver` | Resolve device information |

The server supports multiple session identifier sources through internal resolvers, including cookies, bearer credentials, headers, and query parameters.

The active resolution behavior is controlled by the configured session resolution options.

### Custom Session Validator

```csharp
builder.Services.AddScoped<ISessionValidator, ApplicationSessionValidator>();
builder.Services.AddUltimateAuthServer();
```

A custom validator must preserve the relevant expiration, revocation, and security-state checks.

### Custom Device Resolver

```csharp
builder.Services.AddScoped<IDeviceResolver, ApplicationDeviceResolver>();
builder.Services.AddUltimateAuthServer();
```

Device resolution may affect session identification and security decisions.

Custom implementations should produce consistent, trustworthy device information.

Avoid replacing these services unless the application's requirements cannot be met through standard options.

## 14. Custom Stores

UltimateAuth separates domain behavior from persistence through store interfaces.

Examples include:

```csharp
ISessionStore
IRefreshTokenStore
IRoleStore
IUserRoleStore
```

Some stores are resolved through corresponding factory interfaces.

UltimateAuth provides persistence implementations in separate packages, including in-memory and Entity Framework Core implementations.

### Choosing a Store Implementation

Use in-memory implementations for suitable development, testing, or demonstration scenarios.

Use durable persistence for production authentication state that must survive process restarts or be shared across application instances.

### Custom Store Requirements

A custom store must preserve the semantics expected by the authentication services.

Depending on the store, these may include:

- Correct session expiration handling.
- Reliable revocation state.
- Refresh token consumption and replay protection.
- Consistent user and tenant scoping.
- Appropriate concurrency behavior.
- Atomic state transitions where required.
- Correct deletion and lifecycle behavior.

For example, refresh token rotation may depend on preventing two concurrent requests from successfully consuming the same token.

A store implementation that does not enforce the required concurrency semantics can undermine the security of the refresh flow.

### Store Registration

Custom store registration depends on the specific store interface and its factory.

Do not assume that replacing one store interface is sufficient when the runtime resolves stores through a factory.

Use the relevant domain package's documented registration pattern.

## 15. Authentication Lifecycle Events

UltimateAuth provides application-wide authentication event hooks through:

```csharp
UAuthEvents
```

These hooks allow applications to react to authentication lifecycle events without replacing the authentication flow.

The current event options expose:

```csharp
OnAnyEvent
OnUserLoggedIn
OnUserLoggedOut
```

### Login Event

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Events.OnUserLoggedIn = context =>
    {
        // Record a successful login.
        return Task.CompletedTask;
    };
});
```

### Logout Event

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Events.OnUserLoggedOut = context =>
    {
        // Record a logout event.
        return Task.CompletedTask;
    };
});
```

### Global Event Hook

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Events.OnAnyEvent = context =>
    {
        // Process an authentication event.
        return Task.CompletedTask;
    };
});
```

The global hook receives an `IAuthEventContext`.

### Appropriate Event Uses

Lifecycle events are suitable for:

- Audit logging.
- Application metrics.
- Monitoring.
- Notifications.
- Lightweight post-operation integration.

They should not be used to implement critical authentication checks.

### Events Are Not Decision Hooks

The documented event delegates run after their corresponding successful operations.

For example, `OnUserLoggedIn` is not the place to decide whether the login should have been permitted.

Use an appropriate authority or policy for security decisions.

### Avoid Complex Work Inside Events

Long-running or failure-prone external operations should generally be delegated to a reliable application service or message-processing mechanism.

Do not make critical authentication behavior depend on an unreliable external notification system.

## 16. Custom JWT Signing Keys

UltimateAuth exposes:

```csharp
IJwtSigningKeyProvider
```

The server registers a development signing key provider as its default implementation.

Production applications should supply an appropriate signing key strategy.

For example:

```csharp
builder.Services.AddSingleton<IJwtSigningKeyProvider, ApplicationJwtSigningKeyProvider>();
builder.Services.AddUltimateAuthServer();
```

`ApplicationJwtSigningKeyProvider` is an implementation supplied by the application.

A production implementation should use appropriately protected signing keys and support the application's operational requirements.

### Signing Key Responsibilities

Consider:

- Key confidentiality.
- Key rotation.
- Key identification.
- Consistent validation across resource servers.
- Secure key storage.
- Recovery and deployment procedures.

Do not use development signing keys as a production security strategy.

## 17. Endpoint Customization

UltimateAuth provides an advanced endpoint configuration hook:

```csharp
options.OnConfigureEndpoints
```

The hook receives:

```csharp
IEndpointRouteBuilder
```

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.OnConfigureEndpoints = endpoints =>
    {
        // Apply supported application-specific
        // endpoint configuration here.
    };
});
```

The hook is invoked after UltimateAuth registers its default endpoints.

It can support advanced endpoint integration, but it should not be used to remove, remap, or weaken the security requirements of UltimateAuth endpoints.

### Prefer Endpoint Options First

For ordinary endpoint configuration, use:

```csharp
options.Endpoints
```

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.Authentication = true;
    options.Endpoints.Credentials = true;
    options.Endpoints.Authorization = true;
});
```

The advanced endpoint hook is intended for cases that cannot be addressed through the supported endpoint options.

## 18. Configuration Sources and Precedence

UltimateAuth integrates with the standard ASP.NET Core configuration system.

For the server, the configuration section is:

```text
UltimateAuth:Server
```

For the client:

```text
UltimateAuth:Client
```

For Resource APIs:

```text
UltimateAuth:ResourceApi
```

### Registration Order

The standard server registration applies:

1. Option type defaults.
2. The `AddUltimateAuthServer()` configuration delegate.
3. Values bound from `UltimateAuth:Server`.
4. Post-configuration processing.
5. Registered options validation.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
});
```

With:

```json
{
  "UltimateAuth": {
    "Server": {
      "Login": {
        "MaxFailedAttempts": 3
      }
    }
  }
}
```

The effective configured value is `3`.

The configuration binding occurs after the registration delegate.

### Environment Variables

ASP.NET Core supports hierarchical configuration keys through double underscores.

For example:

```text
UltimateAuth__Server__Login__MaxFailedAttempts=3
```

This corresponds to:

```text
UltimateAuth:Server:Login:MaxFailedAttempts
```

Environment-specific configuration can therefore override values without requiring changes to application code.

### Environment-Specific Files

Standard ASP.NET Core applications commonly use:

```text
appsettings.json
appsettings.Development.json
appsettings.Staging.json
appsettings.Production.json
```

The active environment determines which environment-specific file is loaded.

For example:

```text
ASPNETCORE_ENVIRONMENT=Production
```

The final value depends on the configured ASP.NET Core configuration providers and their precedence.

### Configuration Binding Does Not Replace Mode Resolution

A value supplied through `appsettings.json` becomes part of the configured options.

It may still be replaced later when UltimateAuth applies mode defaults.

For settings explicitly controlled by mode templates, use `ConfigureMode()` when the desired behavior must apply to a specific mode.

## 19. Startup Validation and Runtime Safety

UltimateAuth validates server options through registered `IValidateOptions<UAuthServerOptions>` implementations.

The server uses:

```csharp
ValidateOnStart()
```

This allows many invalid configurations to fail during application startup.

Examples include:

- Invalid endpoint base paths.
- Invalid or unsupported allowed modes.
- Invalid session lifetimes.
- Incompatible token and session lifetime constraints.
- Invalid login settings.
- Invalid tenant configuration.
- Invalid pagination settings.

### Validation Has Boundaries

Startup validation applies to the configured options according to the registered validators.

It does not automatically prove that every custom dependency injection replacement is safe.

It also does not guarantee that every mode-specific override has been validated against all possible security invariants.

Custom implementations must preserve the relevant contracts and security requirements.

### Recommended Validation Strategy

For advanced configurations:

1. Verify that the application starts successfully.
2. Test each enabled authentication mode.
3. Test login, refresh, validation, and logout.
4. Test session expiration and revocation.
5. Test tenant isolation.
6. Test authorization denial paths.
7. Test concurrent refresh requests where applicable.
8. Verify that effective options contain the intended values.

These checks are especially important when customizing authorities, validators, stores, or authentication mode behavior.

## 20. Common Configuration Mistakes

### Configuring Core Options Separately from Server Options

When using `AddUltimateAuthServer()`, configure Core-defined behavior through `UAuthServerOptions`.

Do not expect a separate `UAuthOptions` configuration to override the server's composed options.

### Assuming Global Values Always Survive Mode Defaults

Mode templates can overwrite values such as access token lifetime.

Use `ConfigureMode()` for explicit mode-specific customization.

### Treating AllowedModes as Mode Selection

`AllowedModes` restricts permitted modes.

It does not replace `IEffectiveAuthModeResolver`.

### Enabling an Unsupported Mode

An enum value or default template does not establish that a mode is ready for production.

Use only modes supported by the current implementation.

### Replacing Security Components Unnecessarily

Replacing a validator or authority can change fundamental authentication behavior.

Prefer options and policies when they satisfy the requirement.

### Assuming Every DI Service Is Replaceable

Different services use different registration patterns.

Review the actual registration behavior before replacing a service.

### Using Events for Authorization Decisions

Lifecycle events are for post-operation reactions.

Use authorities and policies for decisions.

### Ignoring Store Concurrency

Authentication stores must preserve the required consistency and atomicity semantics.

This is particularly important for refresh token rotation and revocation.

### Assuming Runtime Overrides Receive Full Startup Validation

Mode-specific overrides are applied during effective-options construction.

Validate their results through tests and appropriate application safeguards.

## 21. Advanced Configuration Reference

| Mechanism | API / Type | Purpose |
|---|---|---|
| Server configuration | `UAuthServerOptions` | Base server settings |
| Mode customization | `ConfigureMode()` | Apply mode-specific overrides |
| Effective configuration | `IEffectiveServerOptionsProvider` | Resolve effective server settings |
| Effective result | `EffectiveUAuthServerOptions` | Resolved mode and options |
| Mode selection | `IEffectiveAuthModeResolver` | Resolve mode from client profile |
| Login decisions | `ILoginAuthority` | Evaluate login decisions |
| Authentication authority | `IAuthAuthority` | Authentication security decisions |
| Access authority | `IAccessAuthority` | Access decisions |
| Tenant identification | `ITenantIdResolver` | Resolve tenant identifiers |
| Session validation | `ISessionValidator` | Validate sessions |
| Session identification | `ISessionIdResolver` | Resolve session identifiers |
| Device identification | `IDeviceResolver` | Resolve device information |
| Signing keys | `IJwtSigningKeyProvider` | Supply JWT signing keys |
| Lifecycle events | `UAuthEvents` | Observe completed authentication operations |
| Endpoint customization | `OnConfigureEndpoints` | Advanced endpoint configuration |
| Session persistence | `ISessionStore` | Session storage |
| Refresh persistence | `IRefreshTokenStore` | Refresh token storage |

## 22. Recommended Extension Strategy

When an application requires customization, follow this order:

1. **Use existing options.** Prefer supported configuration properties.
2. **Use mode overrides.** Apply `ConfigureMode()` when behavior must differ by authentication mode.
3. **Use application policies.** Extend authorization rules without replacing the authorization pipeline.
4. **Use lifecycle events.** Implement logging, monitoring, and notifications without changing authentication decisions.
5. **Use focused resolvers or providers.** Customize tenant identification, signing keys, or other specific infrastructure concerns.
6. **Replace authorities, validators, or stores only when necessary.** Preserve all relevant security invariants and verify the implementation through tests.

The objective is not to replace as much of UltimateAuth as possible.

It is to customize the smallest component necessary while retaining the guarantees of the surrounding system.

## Next Steps

- [Configuration Overview](/docs/configuration/overview) — Understand registration, configuration sources, and architecture.
- [Server Options](/docs/configuration/server-options) — Configure authentication and server security behavior.
- [Client Options](/docs/configuration/client-options) — Configure SDK flows and client behavior.
- [Authentication Modes](/docs/fundamentals/auth-modes) — Understand supported authentication modes.
- [Authorization](/docs/client/authorization) — Use the client authorization API.
- [Session Management](/docs/client/session-management) — Manage sessions and revocation.