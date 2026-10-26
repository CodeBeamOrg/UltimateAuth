---
title: Client Options
order: 3
group: configuration
---

# Client Options

`UAuthClientOptions` controls how an UltimateAuth client communicates with the authentication server, executes authentication flows, maintains client-side authentication state, and responds when authentication must be renewed.

The client configuration is intentionally separate from server security configuration.

The server determines which authentication operations are permitted, how credentials are validated, and when sessions expire. The client determines how those operations are initiated and how the application responds to their outcomes.

This guide covers the current client configuration API, platform integration, defaults, practical examples, and important security considerations.

## 1. Register the UltimateAuth Client

UltimateAuth provides a platform-independent client SDK and platform-specific adapters.

For Blazor applications, use:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoDetectClientProfile = true;
    options.DefaultReturnUrl = "/";
});
```

`AddUltimateAuthClientBlazor()` registers the underlying client services and the Blazor-specific infrastructure, including browser transport, storage, device identification, and authentication state integration.

### Core Client Registration

The platform-independent registration method is:

```csharp
builder.Services.AddUltimateAuthClient(options =>
{
    options.AutoDetectClientProfile = true;
});
```

This registers the shared client SDK.

However, it does not provide every platform-specific implementation needed to run authentication flows.

Applications should use an appropriate platform adapter rather than relying on the core client registration alone.

### Do Not Register Both Client Methods

When using:

```csharp
builder.Services.AddUltimateAuthClientBlazor();
```

do not also call:

```csharp
builder.Services.AddUltimateAuthClient();
```

The Blazor registration already includes the core client services.

### Server and Client in One Application

A Blazor Server application can register both UltimateAuth Server and the Blazor client adapter:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromDays(7);
});

builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.ClientProfile = UAuthClientProfile.BlazorServer;
    options.AutoDetectClientProfile = false;
});
```

These registrations configure different components.

The server controls authentication security and lifecycle rules.

The client controls the application-side integration.

## 2. Client Options Architecture

`UAuthClientOptions` contains the following properties:

| Property | Type | Purpose |
|---|---|---|
| `ClientProfile` | `UAuthClientProfile` | Identifies the client platform |
| `AutoDetectClientProfile` | `bool` | Enables automatic profile detection |
| `DefaultReturnUrl` | `string?` | Default navigation destination after authentication |
| `Endpoints` | `UAuthClientEndpointOptions` | Authentication endpoint paths |
| `Login` | `UAuthClientLoginFlowOptions` | Login flow configuration |
| `Pkce` | `UAuthClientPkceLoginFlowOptions` | PKCE flow configuration |
| `AutoRefresh` | `UAuthClientAutoRefreshOptions` | Background refresh coordination |
| `Reauth` | `UAuthClientReauthOptions` | Reauthentication behavior |
| `MultiTenant` | `UAuthClientMultiTenantOptions` | Tenant propagation |
| `StateEvents` | `UAuthStateEventOptions` | Authentication state event handling |
| `Resource` | `UAuthClientResourceOptions` | Protected Resource API origin and browser credential handling |

These options are configured through the same `UAuthClientOptions` instance.

For example:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.DefaultReturnUrl = "/dashboard";
    options.Pkce.Enabled = true;
    options.AutoRefresh.Interval = TimeSpan.FromMinutes(5);
    options.StateEvents.HandlingMode = UAuthStateEventHandlingMode.Patch;
});
```

## 3. Client Profiles

The client profile identifies the application environment and influences authentication flow behavior.

Configure it through:

```csharp
options.ClientProfile
```

UltimateAuth defines profiles including:

| Profile | Typical application |
|---|---|
| `BlazorServer` | Interactive Blazor Server applications |
| `BlazorWasm` | Browser-hosted Blazor WebAssembly |
| `Maui` | .NET MAUI applications |
| `WebServer` | Traditional ASP.NET Core web applications |
| `Api` | API-oriented clients |
| `UAuthHub` | UltimateAuth Hub |
| `NotSpecified` | Profile not explicitly selected |

Different client profiles may result in different authentication modes on the server.

For example, the default server mode resolver maps Blazor Server to `PureOpaque` and Blazor WebAssembly to `Hybrid`.

The client profile does not itself authorize an authentication mode. The server must support and permit the resolved mode.

## 4. Automatic Client Profile Detection

Automatic profile detection is enabled by default:

```csharp
options.AutoDetectClientProfile = true;
```

When enabled, UltimateAuth detects the client environment if `ClientProfile` remains `NotSpecified`.

The current detector checks for:

1. UAuthHub registration.
2. .NET MAUI.
3. Blazor WebAssembly.
4. Blazor Server.
5. A fallback to `WebServer`.

### Default Configuration

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoDetectClientProfile = true;
});
```

No explicit profile is required for ordinary supported environments where detection succeeds.

### Manual Configuration

For applications that need predictable behavior independent of assembly-based detection:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoDetectClientProfile = false;
    options.ClientProfile = UAuthClientProfile.BlazorWasm;
});
```

Manual configuration can be useful for:

- Specialized hosting environments.
- Integration tests.
- Applications with multiple hosting models.
- Cases where assembly-based detection is ambiguous.

### Important Validation Rule

This configuration is invalid:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoDetectClientProfile = false;
    options.ClientProfile = UAuthClientProfile.NotSpecified;
});
```

When automatic detection is disabled, an explicit client profile must be provided.

### Explicit Profiles Take Precedence

The post-configuration detector does not replace a profile that has already been explicitly selected.

For example:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.ClientProfile = UAuthClientProfile.BlazorServer;
});
```

The explicit profile is retained even when automatic detection remains enabled.

### Detection Limitations

The current detector uses service registration and loaded assemblies to identify the environment.

Assembly-based detection may not be completely reliable in every hosting arrangement.

For complex Blazor Web App hosting scenarios, explicitly configuring the profile can make authentication behavior easier to reason about.

## 5. Endpoint Configuration

`options.Endpoints` defines the endpoint paths used by the client SDK.

The default base path is:

```text
/auth
```

### Configure a Remote Authentication Server

For a client communicating with a separately hosted UAuthHub:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://auth.example.com/auth";
});
```

For an application using same-origin authentication endpoints:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "/auth";
});
```

The base path must match the deployed authentication server's routing configuration.

Changing a client endpoint does not create or expose the corresponding server endpoint.

### Default Endpoint Paths

| Property | Default |
|---|---|
| `BasePath` | `/auth` |
| `Login` | `/login` |
| `TryLogin` | `/try-login` |
| `Logout` | `/logout` |
| `Refresh` | `/refresh` |
| `Reauth` | `/reauth` |
| `Validate` | `/validate` |
| `PkceAuthorize` | `/pkce/authorize` |
| `PkceTryComplete` | `/pkce/try-complete` |
| `PkceComplete` | `/pkce/complete` |
| `HubLoginPath` | `/uauthhub/entry` |

### Customize Individual Paths

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "/auth";
    options.Endpoints.Login = "/login";
    options.Endpoints.Logout = "/logout";
    options.Endpoints.Refresh = "/refresh";
});
```

These values should reflect the actual routes registered by UltimateAuth Server.

### Required Endpoint Validation

The client options validator requires:

- `Endpoints.BasePath`
- `Endpoints.Login`
- `Endpoints.Logout`
- `Endpoints.Refresh`
- `Endpoints.Validate`

These properties must not be null, empty, or whitespace-only.

The current validator does not apply the same required-value checks to every optional endpoint path.

### Remote Clients and Browser Security

When using a remote authentication server, also consider:

- HTTPS.
- Cross-origin request configuration.
- Cookie `SameSite` and `Secure` attributes.
- Allowed client origins.
- PKCE redirects.
- Browser credential policies.

A correct endpoint URL alone does not guarantee that cross-origin authentication will work.

### Resource API Configuration

Authentication endpoints and application resource endpoints serve different purposes.

An application may authenticate through UAuthHub while retrieving business data from a separately deployed Resource API.

UltimateAuth provides `options.Resource` to configure these requests independently.

```csharp
using CodeBeam.UltimateAuth.Client;

builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "https://auth.example.com/auth";

    options.Resource.BaseAddress = "https://api.example.com";
    options.Resource.Credentials = UAuthResourceCredentialsMode.Auto;
});
```

The Resource Client is available through `IUAuthClient.Resource`.

```csharp
var result = await UAuthClient.Resource
    .GetAsync<List<Product>>("/api/products");
```

#### Resource Options

| Property | Default | Description |
|---|---|---|
| `Resource.BaseAddress` | `null` | Resource API origin; `null` uses the application's origin |
| `Resource.Credentials` | `Auto` | Browser credential transmission policy |

#### Base Address Validation

When configured, `Resource.BaseAddress` must be an absolute HTTP or HTTPS origin.

HTTPS is required except for loopback development addresses.

The address must not contain an additional path, query string, fragment, or embedded credentials.

For example:

```csharp
options.Resource.BaseAddress = "https://api.example.com";
```

Individual resource paths are supplied when making requests:

```csharp
await UAuthClient.Resource
    .GetAsync<List<Product>>("/api/products");
```

#### Credential Modes

`UAuthResourceCredentialsMode` supports four options:

| Mode | Description |
|---|---|
| `Auto` | Automatically select the appropriate browser credential policy |
| `SameOrigin` | Include browser credentials only for same-origin requests |
| `Include` | Include browser credentials for the configured Resource API origin |
| `Omit` | Do not include browser credentials |

For example, a separate Resource API that requires cross-origin cookies may use:

```csharp
options.Resource.Credentials = UAuthResourceCredentialsMode.Include;
```

The Resource API must permit the relevant browser origin and support the required CORS and cookie configuration.

Credential configuration does not bypass browser security restrictions or replace server-side authentication and authorization.

#### Resource Requests and Authentication Lifecycle

Resource requests use the browser transport in the Blazor integration.

They support request cancellation, origin validation, and redirect protection.

Resource requests return `UAuthResult<T>` or `UAuthResult`, depending on the operation.

The current Resource Client does not automatically refresh authentication and retry a request after receiving a `401 Unauthorized` response.

For a complete explanation, see [Resource API Requests](/docs/client-sdk/resource-api-requests).

## 6. Login Flow Options

`options.Login` controls client-side login behavior.

The available properties are:

| Property | Default | Purpose |
|---|---|---|
| `ReturnUrl` | `null` | Login-specific return destination |
| `AllowCredentialPost` | `false` | Allow direct credential posting |

### Configure Login Navigation

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Login.ReturnUrl = "/dashboard";
});
```

The login-specific return URL is used when the login flow does not provide a more specific destination.

If it is not set, UltimateAuth can fall back to `DefaultReturnUrl` or the current location according to the flow.

### Direct Credential Posting

The default is:

```csharp
options.Login.AllowCredentialPost = false;
```

This is an important security setting.

Directly posting user credentials is not appropriate for public browser clients such as Blazor WebAssembly applications.

Public clients should use the supported PKCE-based login flow.

### Trusted Server-Hosted Clients

A trusted server-hosted application may explicitly enable direct credential posting where its authentication architecture requires it:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.ClientProfile = UAuthClientProfile.BlazorServer;
    options.Login.AllowCredentialPost = true;
});
```

Do not enable this setting for a public client merely to simplify login implementation.

The server still determines whether the requested authentication operation is permitted.

## 7. PKCE Configuration

`options.Pkce` controls client-side PKCE login behavior.

PKCE allows a public client to complete an authorization flow without relying on a confidential client secret.

The current options are:

| Property | Default | Description |
|---|---|---|
| `Enabled` | `true` | Enable client PKCE support |
| `ReturnUrl` | `null` | Post-login return destination |
| `AutoRedirect` | `true` | Automatically navigate after beginning PKCE |
| `OnAuthorized` | `null` | Callback after the authorization response is issued |

### Basic Configuration

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Pkce.Enabled = true;
    options.Pkce.ReturnUrl = "/dashboard";
    options.Pkce.AutoRedirect = true;
});
```

### Disable Automatic Redirect

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Pkce.AutoRedirect = false;
});
```

When `AutoRedirect` is disabled, `BeginPkceAsync` does not automatically navigate to the next location.

The application is responsible for handling navigation.

This can be useful when integrating UltimateAuth into an existing routing or navigation system.

### Authorization Callback

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Pkce.OnAuthorized = response =>
    {
        // Inspect or process the authorization response.
        return Task.CompletedTask;
    };
});
```

The callback receives:

```csharp
PkceAuthorizeResponse
```

It runs after the authorization response is issued and before the normal Hub navigation.

Use the callback for application-specific integration, not to bypass PKCE verification.

### Client and Server PKCE Settings Are Different

Client configuration:

```csharp
options.Pkce.AutoRedirect = true;
```

Server configuration:

```csharp
options.Pkce.AuthorizationCodeLifetimeSeconds = 120;
```

The client controls how the flow is initiated and navigated.

The server controls the authorization code's validity and verification.

## 8. Return URL Configuration

UltimateAuth supports several return URL configuration points.

| Property | Purpose |
|---|---|
| `DefaultReturnUrl` | Global default for interactive authentication |
| `Login.ReturnUrl` | Login-specific default |
| `Pkce.ReturnUrl` | PKCE-specific default |

### Global Default

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.DefaultReturnUrl = "/dashboard";
});
```

### Flow-Specific Defaults

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.DefaultReturnUrl = "/";
    options.Login.ReturnUrl = "/account";
    options.Pkce.ReturnUrl = "/dashboard";
});
```

Flow-specific values allow different authentication paths to use different destinations.

An explicitly supplied return URL for an individual operation may take precedence over these defaults.

### Security Considerations

Return URLs are navigation destinations and must be treated carefully.

Do not redirect users to arbitrary external URLs without appropriate validation.

Open redirects can be abused for phishing and other attacks.

Prefer local application routes unless cross-origin navigation is an intentional and validated part of the authentication architecture.

## 9. Automatic Refresh

`options.AutoRefresh` controls background refresh coordination.

The available properties are:

| Property | Description |
|---|---|
| `Enabled` | Enables or disables automatic background refresh |
| `Interval` | Interval between background refresh attempts |

### Configure Automatic Refresh

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoRefresh.Enabled = true;
    options.AutoRefresh.Interval = TimeSpan.FromMinutes(5);
});
```

### Disable Automatic Refresh

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoRefresh.Enabled = false;
});
```

### Default Interval

If `Interval` is not supplied, the client registration applies:

```csharp
options.AutoRefresh.Interval ??= TimeSpan.FromMinutes(5);
```

The resulting default interval is five minutes.

### Platform Behavior

The effective `Enabled` setting should be considered together with the platform's post-configuration and session coordination behavior.

The current Blazor session coordinator checks `AutoRefresh.Enabled` before starting its background loop.

When enabled, it periodically invokes:

```csharp
UAuthClient.Flows.RefreshAsync(true);
```

### Automatic Refresh Is Not a Security Policy

Automatic refresh does not control:

- Server session lifetime.
- Refresh token lifetime.
- Session revocation.
- Credential recovery policies.
- Refresh token replay protection.

These remain server-side responsibilities.

### Automatic Refresh Does Not Guarantee Session Continuity

Background refresh may fail because:

- The session has expired.
- The refresh credential has been revoked.
- The browser is suspended.
- Network connectivity is unavailable.
- The server requires reauthentication.

Automatic refresh is a convenience and continuity mechanism, not a guarantee that authentication remains valid indefinitely.

### Coordinator Lifecycle

The Blazor adapter provides an `ISessionCoordinator` implementation.

The coordinator must be started by the application's supported initialization lifecycle.

Setting `AutoRefresh.Enabled = true` does not mean that a background task automatically runs without the coordinator being started.

Applications should follow the client initialization guidance for their hosting model.

## 10. Reauthentication Options

`options.Reauth` controls how the Blazor session coordinator responds when refresh reports that reauthentication is required.

The available properties are:

| Property | Default |
|---|---|
| `Behavior` | `ReauthBehavior.Redirect` |
| `RedirectPath` | `null` |

### Available Behaviors

```csharp
ReauthBehavior.Redirect
ReauthBehavior.None
ReauthBehavior.RaiseEvent
```

### Redirect

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Reauth.Behavior = ReauthBehavior.Redirect;
    options.Reauth.RedirectPath = "/login";
});
```

When the coordinator receives a `ReauthRequired` refresh outcome, it navigates to the configured redirect path.

If `RedirectPath` is null, the coordinator falls back to `Endpoints.Login`.

The current Blazor coordinator uses forced navigation for this redirect.

### Raise an Event

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Reauth.Behavior = ReauthBehavior.RaiseEvent;
});
```

This allows an application to handle reauthentication through the coordinator's `ReauthRequired` event.

For example, an application may display a reauthentication dialog rather than immediately navigating.

### No Automatic Action

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Reauth.Behavior = ReauthBehavior.None;
});
```

With `None`, the coordinator does not automatically redirect or raise its reauthentication event.

The application must still handle unauthenticated states appropriately.

### Important

Reauthentication behavior does not make an expired or revoked session valid.

It only controls how the client responds when authentication must be reestablished.

## 11. Multi-Tenant Client Configuration

`options.MultiTenant` controls tenant propagation from the client to the authentication server.

The client does not independently authorize or resolve tenant ownership.

It carries tenant context using the configured transport.

### Available Properties

| Property | Default | Description |
|---|---|---|
| `Enabled` | `false` | Enable tenant propagation |
| `Tenant` | `null` | Tenant identifier to propagate |
| `Transport` | `TenantTransport.None` | Tenant transport mechanism |

### Available Transports

```csharp
TenantTransport.None
TenantTransport.Header
TenantTransport.Route
```

### Header-Based Tenant Propagation

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.MultiTenant.Enabled = true;
    options.MultiTenant.Tenant = "tenant-a";
    options.MultiTenant.Transport = TenantTransport.Header;
});
```

The server must be configured to resolve tenant information from the corresponding transport.

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.MultiTenant.Enabled = true;
    options.MultiTenant.EnableHeader = true;
});
```

### Route-Based Tenant Propagation

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.MultiTenant.Enabled = true;
    options.MultiTenant.Tenant = "tenant-a";
    options.MultiTenant.Transport = TenantTransport.Route;
});
```

The server must expose routes compatible with tenant-based routing.

### Security Boundary

A client-provided tenant identifier is not proof of tenant membership.

The server must validate the resolved tenant and enforce tenant isolation.

Never rely on client-side tenant selection as an authorization boundary.

## 12. Authentication State Events

`options.StateEvents` controls how the client processes authentication-related state events.

The available property is:

```csharp
options.StateEvents.HandlingMode
```

Its default is:

```csharp
UAuthStateEventHandlingMode.Patch
```

### Available Modes

| Mode | Behavior |
|---|---|
| `Patch` | Apply a local authentication state update |
| `Validate` | Revalidate authentication state after the event |
| `None` | Do not update authentication state in response to the event |

### Patch

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.StateEvents.HandlingMode = UAuthStateEventHandlingMode.Patch;
});
```

`Patch` is the default.

It allows the client to update local state without performing a full authentication validation for every event.

### Validate

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.StateEvents.HandlingMode = UAuthStateEventHandlingMode.Validate;
});
```

`Validate` causes the client to revalidate authentication state in response to events.

This may introduce additional requests but can be appropriate when the application prefers server-confirmed state updates.

### None

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.StateEvents.HandlingMode = UAuthStateEventHandlingMode.None;
});
```

`None` disables automatic authentication state updates in response to these events.

Applications using this mode must provide any required state synchronization through other mechanisms.

### Local Events Are Not Distributed Notifications

Client state events do not automatically notify every browser, device, or application instance.

For example, an administrator changing a user's role does not automatically push an event to that user's open browser.

Distributed synchronization requires additional application infrastructure.

## 13. Configuration from appsettings.json

UltimateAuth Client binds the configuration section:

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
      "Endpoints": {
        "BasePath": "https://auth.example.com/auth"
      },
      "Pkce": {
        "Enabled": true,
        "ReturnUrl": "/dashboard",
        "AutoRedirect": true
      },
      "AutoRefresh": {
        "Enabled": true,
        "Interval": "00:05:00"
      },
      "Reauth": {
        "Behavior": "Redirect",
        "RedirectPath": "/login"
      },
      "StateEvents": {
        "HandlingMode": "Patch"
      }
    }
  }
}
```

The example uses the standard ASP.NET Core configuration binding conventions.

### Configuration Precedence

The registration pipeline applies:

1. Option type defaults.
2. The configuration delegate.
3. Values from `UltimateAuth:Client`.
4. Post-configuration processing.
5. Registered options validation when the options are resolved.

For example:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.DefaultReturnUrl = "/";
});
```

With:

```json
{
  "UltimateAuth": {
    "Client": {
      "DefaultReturnUrl": "/dashboard"
    }
  }
}
```

The configured return URL becomes `/dashboard`.

### Environment Variables

ASP.NET Core supports double underscores for hierarchical configuration keys.

For example:

```text
UltimateAuth__Client__Endpoints__BasePath=https://auth.example.com/auth
```

This corresponds to:

```text
UltimateAuth:Client:Endpoints:BasePath
```

Use environment-specific configuration for deployment-dependent values such as authentication server addresses and application return paths.

## 14. Client Options Validation

UltimateAuth registers validators for:

```csharp
UAuthClientOptions
UAuthClientEndpointOptions
```

### Client Profile Validation

The validator rejects a configuration where:

```csharp
options.AutoDetectClientProfile = false;
options.ClientProfile = UAuthClientProfile.NotSpecified;
```

An explicit profile is required when detection is disabled.

### Endpoint Validation

The following endpoint properties must be present:

```text
Endpoints.BasePath
Endpoints.Login
Endpoints.Logout
Endpoints.Refresh
Endpoints.Validate
```

For example, this configuration is invalid:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.Endpoints.BasePath = "";
});
```

### Validation Timing

Unlike the server registration, the current client registration does not explicitly call `ValidateOnStart()`.

Validation therefore occurs through the normal options resolution lifecycle.

Applications that require early validation can arrange for the configured options to be resolved during startup.

### Validation Scope

The current validators check essential client profile and endpoint requirements.

They do not automatically validate every possible combination of client flow, server deployment, cookie behavior, and authentication mode.

Applications should test their actual authentication flows against the deployed server configuration.

## 15. Complete Blazor WebAssembly Example

The following example configures a Blazor WebAssembly client using a remote authentication server.

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    // Client profile
    options.AutoDetectClientProfile = false;
    options.ClientProfile = UAuthClientProfile.BlazorWasm;

    // Authentication server
    options.Endpoints.BasePath = "https://auth.example.com/auth";

    // Navigation
    options.DefaultReturnUrl = "/";
    options.Login.ReturnUrl = "/dashboard";

    // Public client security
    options.Login.AllowCredentialPost = false;

    // PKCE
    options.Pkce.Enabled = true;
    options.Pkce.ReturnUrl = "/dashboard";
    options.Pkce.AutoRedirect = true;

    // Background refresh
    options.AutoRefresh.Enabled = false;

    // Reauthentication
    options.Reauth.Behavior = ReauthBehavior.Redirect;
    options.Reauth.RedirectPath = "/login";

    // State updates
    options.StateEvents.HandlingMode = UAuthStateEventHandlingMode.Patch;
});
```

This example emphasizes PKCE-based authentication and avoids direct credential posting from a public browser client.

The remote server must be configured with compatible endpoints, origins, authentication modes, and credential delivery policies.

## 16. Complete Blazor Server Example

The following example configures a Blazor Server client.

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    // Client profile
    options.AutoDetectClientProfile = false;
    options.ClientProfile = UAuthClientProfile.BlazorServer;

    // Same-origin authentication endpoints
    options.Endpoints.BasePath = "/auth";

    // Navigation
    options.DefaultReturnUrl = "/";
    options.Login.ReturnUrl = "/dashboard";

    // Background refresh
    options.AutoRefresh.Enabled = true;
    options.AutoRefresh.Interval = TimeSpan.FromMinutes(5);

    // Reauthentication
    options.Reauth.Behavior = ReauthBehavior.Redirect;
    options.Reauth.RedirectPath = "/login";

    // Authentication state
    options.StateEvents.HandlingMode = UAuthStateEventHandlingMode.Patch;
});
```

Blazor Server applications can use session-oriented authentication and the corresponding server-side security policies.

The application must still initialize the client and session coordinator through its supported lifecycle.

## 17. Server and Client Configuration Together

An application that hosts both UltimateAuth Server and the Blazor client adapter can configure them independently.

```csharp
// Server: authentication and security policies
builder.Services.AddUltimateAuthServer(options =>
{
    options.AllowedModes =
    [
        UAuthMode.PureOpaque,
        UAuthMode.Hybrid
    ];

    options.Session.Lifetime = TimeSpan.FromDays(7);
    options.Login.MaxFailedAttempts = 5;
});

// Client: platform integration and flow behavior
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.ClientProfile = UAuthClientProfile.BlazorServer;
    options.AutoDetectClientProfile = false;
    options.Endpoints.BasePath = "/auth";
    options.AutoRefresh.Enabled = true;
});
```

The two configuration objects have different responsibilities.

| Requirement | Configure on |
|---|---|
| Session lifetime | Server |
| Access token lifetime | Server |
| Refresh token replay policy | Server |
| Credential recovery rules | Server |
| Allowed authentication modes | Server |
| Client profile | Client |
| PKCE navigation | Client |
| Automatic background refresh | Client |
| Reauthentication navigation | Client |
| Authentication state event handling | Client |
| Tenant validation | Server |
| Tenant propagation | Client |

The client cannot override server-side security decisions.

## 18. Common Configuration Mistakes

### Registering the Core Client and Blazor Adapter Separately

Use `AddUltimateAuthClientBlazor()` for Blazor applications.

It already registers the underlying client SDK.

### Disabling Profile Detection Without Selecting a Profile

When `AutoDetectClientProfile` is false, set `ClientProfile` explicitly.

### Relying on Automatic Detection in an Ambiguous Hosting Environment

For complex hosting arrangements, consider specifying the client profile explicitly.

### Enabling Direct Credential Posting in a Public Client

Do not enable `AllowCredentialPost` for browser-hosted public clients.

Use PKCE.

### Assuming Client Auto-Refresh Extends Server Lifetimes

Automatic refresh only initiates refresh operations.

The server controls whether refresh is permitted.

### Assuming Auto-Refresh Starts Automatically

The Blazor session coordinator must be started through the supported application lifecycle.

### Configuring Only the Client Endpoint

The server must expose compatible routes and allow the intended cross-origin interaction.

### Treating Tenant Propagation as Authorization

A tenant identifier supplied by the client does not grant access to that tenant.

### Using State Events as Distributed Notifications

Client events update local client state; they do not automatically synchronize all connected devices.

### Assuming Every Client Setting Is Validated at Startup

The current client registration validates options when they are resolved, rather than explicitly requiring startup validation.

## 19. Client Options Quick Reference

| Option | Default | Description |
|---|---|---|
| `ClientProfile` | `NotSpecified` | Client environment |
| `AutoDetectClientProfile` | `true` | Automatic profile detection |
| `DefaultReturnUrl` | `null` | Global return URL |
| `Endpoints.BasePath` | `/auth` | Authentication endpoint base |
| `Login.ReturnUrl` | `null` | Login return URL |
| `Login.AllowCredentialPost` | `false` | Direct credential posting |
| `Pkce.Enabled` | `true` | PKCE flow support |
| `Pkce.ReturnUrl` | `null` | PKCE return URL |
| `Pkce.AutoRedirect` | `true` | Automatic PKCE navigation |
| `Pkce.OnAuthorized` | `null` | PKCE authorization callback |
| `AutoRefresh.Interval` | 5 minutes after post-configuration | Background refresh interval |
| `Reauth.Behavior` | `Redirect` | Reauthentication handling |
| `Reauth.RedirectPath` | `null` | Reauthentication redirect |
| `MultiTenant.Enabled` | `false` | Tenant propagation |
| `MultiTenant.Tenant` | `null` | Tenant identifier |
| `MultiTenant.Transport` | `None` | Tenant transport |
| `StateEvents.HandlingMode` | `Patch` | State event handling |
| `Resource.BaseAddress` | `null` | Resource API origin; defaults to application origin |
| `Resource.Credentials` | `Auto` | Browser credential handling for resource requests |

`AutoRefresh.Enabled` should be evaluated with the applicable platform and post-configuration behavior rather than inferred solely from its property initializer.

## Next Steps

- **Configuration Overview** — Understand the configuration architecture and source precedence.
- **Server Options** — Configure authentication security, sessions, tokens, and endpoints.
- **Advanced Configuration** — Explore effective options, mode resolution, and extension points.
- **Client Authentication** — Implement login, PKCE, refresh, and logout flows.
- **Client Session Management** — Manage authenticated sessions and revocation.