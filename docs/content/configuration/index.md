---
title: Configuration Overview
order: 1
group: configuration
---

# Configuration & Extensibility

UltimateAuth provides a structured configuration system for controlling authentication behavior, session lifecycles, token policies, client integration, and server security.

Configuration is organized around the application components that actually consume it.

For most applications, the two primary configuration types are:

- **`UAuthServerOptions`** — Controls authentication and security behavior on the UltimateAuth server.
- **`UAuthClientOptions`** — Controls how an application client communicates with UltimateAuth and executes authentication flows.

UltimateAuth also defines reusable options in its Core package. These types form the foundation of server configuration, but they do not need to be configured independently when using `AddUltimateAuthServer()`.

This guide explains where configuration belongs, how options are registered, how configuration sources interact, and how to customize behavior safely.

## 1. Configuration Architecture

UltimateAuth separates reusable option definitions from the runtime components that consume them.

```text
UltimateAuth
│
├── Core
│   └── Shared option definitions
│       ├── Login
│       ├── Session
│       ├── Token
│       ├── PKCE
│       └── Multi-tenancy
│
├── Server
│   └── UAuthServerOptions
│       ├── Core-defined options
│       ├── Authentication mode restrictions
│       ├── Cookie and session resolution
│       ├── Endpoints
│       ├── Hub configuration
│       ├── Credential policies
│       └── Server-specific behavior
│
└── Client
    └── UAuthClientOptions
        ├── Client profile
        ├── Login and PKCE flows
        ├── Endpoint configuration
        ├── Auto-refresh
        ├── Reauthentication
        └── Client state events
```

The important distinction is between **where an option type is defined** and **where its value is consumed**.

For example, `UAuthSessionOptions` is defined in Core, but a normal UltimateAuth Server application configures it through:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromHours(8);
});
```

The server consumes `options.Session` from `UAuthServerOptions`.

There is no need to configure a separate Core options instance for this setting.

## 2. Configure the Server

The main server registration method is:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    // Server configuration.
});
```

This registers the UltimateAuth server infrastructure and its configuration.

The method accepts an optional configuration delegate:

```csharp
Action<UAuthServerOptions>
```

It also supports a separate optional delegate for registering access policies.

### Example: Authentication and Session Settings

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;

    options.Session.Lifetime = TimeSpan.FromHours(8);

    options.Token.AccessTokenLifetime =
        TimeSpan.FromMinutes(15);
});
```

These settings control different parts of the authentication lifecycle:

| Option | Purpose |
|---|---|
| `Login.MaxFailedAttempts` | Maximum failed login attempts under the configured login policy |
| `Session.Lifetime` | Session lifetime |
| `Token.AccessTokenLifetime` | Access token lifetime |

The server applies its validation rules to the resulting configuration.

**Configuration values should be chosen together.** For example, token lifetime settings must remain compatible with the configured session lifetime limits.

## 3. Core Options Are Composed into Server Options

`UAuthServerOptions` includes the following Core-defined option groups:

```csharp
public UAuthLoginOptions Login { get; set; } = new();

public UAuthSessionOptions Session { get; set; } = new();

public UAuthTokenOptions Token { get; set; } = new();

public UAuthPkceOptions Pkce { get; set; } = new();

public UAuthMultiTenantOptions MultiTenant { get; set; } = new();
```

This is composition, not duplication.

The Core package owns the shared option definitions, while the Server package uses those definitions as part of its effective server configuration.

### Recommended

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromHours(8);
    options.Pkce.Enabled = true;
});
```

### Not Recommended for Server Configuration

```csharp
builder.Services.AddUltimateAuth(options =>
{
    options.Session.Lifetime = TimeSpan.FromHours(8);
});

builder.Services.AddUltimateAuthServer();
```

The second example configures `UAuthOptions`, not the `UAuthServerOptions` instance consumed by the server's authentication runtime.

**When using UltimateAuth Server, configure Core-defined behavior through `AddUltimateAuthServer()` or the `UltimateAuth:Server` configuration section.**

Direct Core configuration is intended for scenarios that explicitly consume `UAuthOptions` rather than the server configuration model.

## 4. Server Configuration Groups

`UAuthServerOptions` organizes server behavior into focused option groups.

| Property | Responsibility |
|---|---|
| `Login` | Login attempts and authentication behavior |
| `Session` | Session lifetime and lifecycle |
| `Token` | Token issuance, lifetimes, and refresh behavior |
| `Pkce` | PKCE flow configuration |
| `MultiTenant` | Tenant resolution and multi-tenant behavior |
| `AllowedModes` | Restricts permitted authentication modes |
| `HubDeploymentMode` | Hub deployment arrangement |
| `Hub` | Hub-specific configuration |
| `Cookie` | Authentication cookie behavior |
| `SessionResolution` | Resolving session identifiers from incoming requests |
| `Endpoints` | Server endpoint configuration |
| `CredentialSessionPolicy` | Session handling after credential changes and recovery |
| `ResetCredential` | Credential reset configuration |
| `PrimaryCredential` | Primary credential policy |
| `Identifiers` | Identifier management behavior |
| `IdentifierValidation` | Identifier validation rules |
| `LoginIdentifiers` | Identifier types permitted for login |
| `UserProfile` | User profile behavior |
| `Pagination` | Pagination limits and defaults |
| `AuthResponse` | Authentication response behavior |
| `Navigation` | Navigation and redirect behavior |
| `Diagnostics` | Diagnostic configuration |
| `Events` | Server event configuration |

These groups allow applications to customize specific behavior without replacing the overall authentication architecture.

The individual option types and their defaults are documented in [Server Options](/docs/configuration/server-options).

## 5. Configure the Client

UltimateAuth clients have their own configuration model.

```csharp
builder.Services.AddUltimateAuthClient(options =>
{
    options.AutoDetectClientProfile = true;
    options.DefaultReturnUrl = "/";
});
```

The registration accepts:

```csharp
Action<UAuthClientOptions>
```

The client options control how the client behaves, not the server's security policy.

For example:

```csharp
builder.Services.AddUltimateAuthClient(options =>
{
    options.AutoRefresh.Enabled = true;
    options.DefaultReturnUrl = "/dashboard";
});
```

Enabling client auto-refresh does not change the server's refresh token lifetime, session expiration rules, or rotation policy.

Those are server-side responsibilities.

### Client Configuration Groups

| Property | Responsibility |
|---|---|
| `ClientProfile` | Explicit client profile |
| `AutoDetectClientProfile` | Automatic client profile detection |
| `DefaultReturnUrl` | Default post-authentication navigation |
| `Login` | Client login flow behavior |
| `Pkce` | Client PKCE flow behavior |
| `AutoRefresh` | Automatic refresh behavior |
| `Reauth` | Reauthentication behavior |
| `Endpoints` | Server endpoint paths used by the client |
| `MultiTenant` | Client tenant propagation |
| `StateEvents` | Client state event handling |

See [Client Options](/docs/configuration/client-options) for the full reference.

## 6. Server and Client Configuration Are Independent

A project may register both UltimateAuth Server and UltimateAuth Client.

This is common in applications that host authentication services and also use the client SDK.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromHours(8);
});

builder.Services.AddUltimateAuthClient(options =>
{
    options.AutoDetectClientProfile = true;
    options.DefaultReturnUrl = "/";
});
```

These registrations configure different components.

| Setting | Server | Client |
|---|---|---|
| Session lifetime | Controls | Does not control |
| Token lifetime | Controls | Does not control |
| Credential reset policy | Controls | Does not control |
| Allowed authentication modes | Controls | Does not control |
| Client profile | Does not control | Controls |
| Client auto-refresh | Does not control | Controls |
| Client navigation | Does not control | Controls |
| Server endpoint exposure | Controls | Uses configured routes |

The client must be compatible with the server's configuration, but it cannot override server-side security requirements.

For example, enabling PKCE in client options does not make a disabled or unsupported PKCE server flow available.

## 7. Configuration Sources

UltimateAuth supports configuration through registration delegates and the standard ASP.NET Core configuration system.

### Code-Based Configuration

Use `Program.cs` for explicit configuration.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Session.Lifetime = TimeSpan.FromHours(8);
});
```

This is useful when configuration is closely tied to application behavior or requires strongly typed values.

### Configuration-Based Setup

The server binds settings from:

```text
UltimateAuth:Server
```

For example, in `appsettings.json`:

```json
{
  "UltimateAuth": {
    "Server": {
      "Login": {
        "MaxFailedAttempts": 5
      },
      "Session": {
        "Lifetime": "08:00:00"
      }
    }
  }
}
```

The client binds settings from:

```text
UltimateAuth:Client
```

For example:

```json
{
  "UltimateAuth": {
    "Client": {
      "AutoDetectClientProfile": true,
      "DefaultReturnUrl": "/dashboard",
      "AutoRefresh": {
        "Enabled": true
      }
    }
  }
}
```

These sections are independent.

### Using Both Sources

You can combine code-based and configuration-based settings:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Session.Lifetime = TimeSpan.FromHours(8);
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

The effective `Login.MaxFailedAttempts` is `3`, because the registration binds the configuration section after applying the configuration delegate.

The session lifetime remains eight hours because the configuration section does not override it.

## 8. Configuration Precedence

The current registration pipeline applies options in this order:

1. Option type defaults.
2. Values supplied by the registration delegate.
3. Values bound from the corresponding configuration section.
4. Applicable post-configuration processing.
5. Options validation.

For the server:

```csharp
services.AddOptions<UAuthServerOptions>()
    .Configure(options =>
    {
        configure?.Invoke(options);
    })
    .BindConfiguration("UltimateAuth:Server")
    .ValidateOnStart();
```

For the client, the corresponding configuration section is `UltimateAuth:Client`.

### Important Distinction

When we say configuration values override `Program.cs`, we mean that the **later configuration binding takes precedence for the same property**.

The exact value also depends on the normal ASP.NET Core configuration provider order.

For example, environment-specific configuration files and environment variables may override values from the base `appsettings.json` file.

### Environment Variable Example

ASP.NET Core configuration supports double underscores as section separators.

For example:

```text
UltimateAuth__Server__Login__MaxFailedAttempts=3
```

This maps to:

```text
UltimateAuth:Server:Login:MaxFailedAttempts
```

For a client setting:

```text
UltimateAuth__Client__DefaultReturnUrl=/dashboard
```

This makes it possible to provide environment-specific settings without modifying application code.

## 9. Authentication Mode Configuration

UltimateAuth supports multiple authentication mode concepts.

The server exposes:

```csharp
options.AllowedModes
```

This setting defines which modes the server is permitted to use.

It is a **restriction**, not a direct mode selector.

### Restrict Allowed Modes

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

The effective authentication mode is resolved by the server's authentication mode resolution mechanism.

Setting `AllowedModes` does not directly choose which mode every client will use.

### Unsupported Modes

In the current implementation, `SemiHybrid` and `PureJwt` are not yet accepted as enabled server modes.

The server options validator rejects them when explicitly included in `AllowedModes`.

For example, this configuration is invalid:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.AllowedModes =
    [
        UAuthMode.PureJwt
    ];
});
```

Do not enable an unsupported mode merely because it exists in the enum.

For supported modes and their behavior, see [Authentication Modes](/docs/fundamentals/authentication-modes).

## 10. Configure Credential Session Policies

UltimateAuth supports configurable session invalidation after credential changes.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.CredentialSessionPolicy.OnSelfChange =
        CredentialSessionBehavior.PreserveCurrent;

    options.CredentialSessionPolicy.OnAdminChange =
        CredentialSessionBehavior.RevokeAll;

    options.CredentialSessionPolicy.OnRecovery =
        CredentialSessionBehavior.RevokeAll;
});
```

The available behaviors are:

| Behavior | Meaning |
|---|---|
| `PreserveAll` | Preserve existing session chains |
| `PreserveCurrent` | Preserve the current chain and revoke others |
| `RevokeAll` | Revoke all applicable session chains |

This is an example of configuration controlling **security behavior**, rather than merely adjusting a numeric value.

The client does not decide which sessions remain valid after a credential change.

That decision belongs to the server.

## 11. Configure Endpoint Exposure

UltimateAuth allows server applications to configure endpoint groups.

The relevant options are available through:

```csharp
options.Endpoints
```

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.BasePath = "/uauth";
});
```

The client must use compatible endpoint paths.

```csharp
builder.Services.AddUltimateAuthClient(options =>
{
    options.Endpoints.BasePath = "/uauth";
});
```

The server controls which endpoints are registered and exposed.

The client controls where it sends requests.

Changing a client endpoint path does not create or enable a server endpoint.

See [Server Options](/docs/configuration/server-options) and [Client Options](/docs/configuration/client-options).

## 12. Multi-Tenant Configuration

UltimateAuth supports tenant-aware authentication and authorization.

The server's multi-tenant configuration is available through:

```csharp
options.MultiTenant
```

The server uses this configuration to determine how tenant information is resolved.

Supported resolution mechanisms include:

- Route-based resolution.
- Header-based resolution.
- Host-based resolution.

The corresponding server options include:

```csharp
options.MultiTenant.EnableRoute;
options.MultiTenant.EnableHeader;
options.MultiTenant.EnableDomain;
```

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.MultiTenant.EnableHeader = true;
});
```

The client has its own multi-tenant options for propagating tenant information to the server.

Client-side tenant selection does not authorize cross-tenant access.

The server remains responsible for tenant resolution, validation, and isolation.

For more details, see [Advanced Configuration](/docs/configuration/advanced-configuration).

## 13. Resource API Configuration

UltimateAuth also supports a separate Resource API integration.

Resource APIs use their own configuration model:

```csharp
UAuthResourceApiOptions
```

Register a Resource API with:

```csharp
builder.Services.AddUltimateAuthResourceApi(options =>
{
    options.UAuthHubBaseUrl = "https://auth.example.com";
});
```

The configuration section is:

```text
UltimateAuth:ResourceApi
```

For example:

```json
{
  "UltimateAuth": {
    "ResourceApi": {
      "UAuthHubBaseUrl": "https://auth.example.com"
    }
  }
}
```

A Resource API does not become a full UltimateAuth authentication server merely by registering this integration.

Its role is to protect application resources using the configured UltimateAuth authentication infrastructure.

### Important: Resource API CORS Configuration

The current Resource API registration constructs its CORS policy using the values supplied to the registration delegate.

Therefore, when configuring `AllowedClientOrigins`, prefer supplying them directly in the registration delegate rather than assuming values bound only from `appsettings.json` will automatically populate that CORS policy.

For example:

```csharp
builder.Services.AddUltimateAuthResourceApi(options =>
{
    options.UAuthHubBaseUrl = "https://auth.example.com";
    options.AllowedClientOrigins.Add("https://app.example.com");
});
```

Keep CORS configuration explicit and restrict allowed origins to trusted applications.

## 14. Validation and Safe Configuration

UltimateAuth uses ASP.NET Core options validation to reject invalid configurations.

The server registers validators for areas including:

- General server configuration.
- Login settings.
- Session settings.
- Token settings.
- Multi-tenancy.
- User identifiers.
- Session resolution.
- Pagination.

For example, the server validates that:

- The endpoint base path is specified.
- The base path does not contain invalid double slashes.
- Allowed authentication modes are valid.
- Explicitly unsupported modes are not enabled.
- Session lifetimes satisfy applicable constraints.
- Access token lifetime does not exceed the configured session maximum lifetime.

### Example: Invalid Session Lifetime

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.Zero;
});
```

This configuration is rejected when the applicable session lifetime validation runs.

### Example: Invalid Endpoint Path

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.BasePath = "//invalid";
});
```

This configuration fails server options validation.

### Validation Timing

Server options use `ValidateOnStart()`.

This allows configuration errors to surface during application startup instead of waiting until the first authentication request.

Client options also have registered validators and post-configuration processing, but their validation timing follows the client options registration and resolution lifecycle.

**Validation is an important safeguard, not a guarantee that every logically undesirable configuration is automatically detected.**

Applications must still choose appropriate production settings and securely implement custom extensions.

## 15. Extensibility Through Dependency Injection

UltimateAuth uses dependency injection to support customization of selected runtime behaviors.

Configuration controls the behavior of existing components.

Dependency injection allows applications to supply implementations for supported extension points.

Examples include:

| Extension point | Purpose |
|---|---|
| `ICredentialResetNotifier` | Deliver credential recovery proofs |
| `IUserIdentifierVerifier` | Validate identifier ownership proof |
| `ITenantIdResolver` | Customize tenant identification |
| `IClock` | Provide a clock implementation |
| `IJwtSigningKeyProvider` | Supply JWT signing keys |

For example, a credential reset notifier can be registered as:

```csharp
builder.Services.AddScoped<ICredentialResetNotifier, ApplicationCredentialResetNotifier>();
```

`ApplicationCredentialResetNotifier` represents an implementation supplied by your application.

The server's supported registration and resolution behavior must be respected when replacing services.

Not every internal service is intended to be replaced, and custom implementations must preserve UltimateAuth's security invariants.

### Configuration Versus Extension

Use configuration when you want to adjust a supported behavior:

```csharp
options.Session.Lifetime = TimeSpan.FromHours(8);
```

Use an extension point when your application needs to provide its own implementation:

```csharp
builder.Services.AddScoped<ICredentialResetNotifier, ApplicationCredentialResetNotifier>();
```

This separation helps keep ordinary configuration simple while allowing advanced integration where necessary.

## 16. Endpoint Customization

The server exposes an advanced endpoint configuration hook:

```csharp
options.OnConfigureEndpoints
```

The hook receives an `IEndpointRouteBuilder`.

It is intended for advanced endpoint configuration after UltimateAuth registers its default endpoints.

For example, an application may use the hook to apply additional endpoint metadata or filters where appropriate.

However, it must not remove or remap UltimateAuth endpoints in ways that invalidate expected security behavior.

Endpoint customization should be treated as an advanced integration mechanism, not a replacement for the supported endpoint configuration options.

See [Advanced Configuration](/docs/configuration/advanced-configuration).

## 17. Recommended Configuration Strategy

For most applications, use the following approach.

### Keep Server Security Settings on the Server

Configure authentication modes, token lifetimes, session lifetimes, credential policies, and tenant resolution through `UAuthServerOptions`.

### Keep Client Behavior in Client Options

Configure client profiles, navigation, auto-refresh, reauthentication, and endpoint usage through `UAuthClientOptions`.

### Use Configuration Files for Environment Differences

Use `appsettings.json`, environment-specific configuration, and environment variables for values that differ between development, staging, and production.

### Prefer Strongly Typed Configuration

Use the provided option types rather than inventing custom configuration keys for features already supported by UltimateAuth.

### Preserve Secure Defaults

Only change security-sensitive defaults when the application's requirements justify the change.

### Avoid Duplicate Core Configuration

When using `AddUltimateAuthServer()`, do not separately configure Core options and expect them to override the server's composed options.

### Validate Production Configuration

Test startup validation and authentication flows using the effective configuration that will be deployed.

## 18. Configuration Reference

| Application role | Registration method | Options type | Configuration section |
|---|---|---|---|
| Authentication server | `AddUltimateAuthServer()` | `UAuthServerOptions` | `UltimateAuth:Server` |
| UltimateAuth client | `AddUltimateAuthClient()` | `UAuthClientOptions` | `UltimateAuth:Client` |
| Resource API | `AddUltimateAuthResourceApi()` | `UAuthResourceApiOptions` | `UltimateAuth:ResourceApi` |
| Core-only infrastructure | `AddUltimateAuth()` | `UAuthOptions` | `UltimateAuth:Core` |

For normal server applications, **`UAuthServerOptions` is the effective configuration entry point for both Core-defined authentication settings and server-specific behavior**.

The Core-only registration exists for infrastructure scenarios that explicitly consume `UAuthOptions`; it should not be treated as an additional server configuration layer.

## Next Steps

- [Configuration Overview](/docs/configuration/configuration-overview) — Understand the overall configuration model.
- [Core Options](/docs/configuration/core-options) — Explore the reusable option definitions composed into server configuration.
- [Server Options](/docs/configuration/server-options) — Configure authentication and security behavior.
- [Client Options](/docs/configuration/client-options) — Configure client profiles, flows, and SDK behavior.
- [Advanced Configuration](/docs/configuration/advanced-configuration) — Customize integration, multi-tenancy, and runtime extension points.