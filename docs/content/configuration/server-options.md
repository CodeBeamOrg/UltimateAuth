---
title: Server Options
order: 2
group: configuration
---

# Server Options

`UAuthServerOptions` is the primary configuration surface for UltimateAuth Server.

It controls authentication behavior, session lifecycles, token issuance, credential security, tenant resolution, endpoint exposure, and integration with UAuthHub.

UltimateAuth composes its Core-defined options directly into `UAuthServerOptions`. When using UltimateAuth Server, you configure both shared authentication behavior and server-specific features through the same options object.

This guide covers the available configuration groups, their defaults, practical examples, and the distinction between configured and effective runtime behavior.

## 1. Register UltimateAuth Server

Configure the server through `AddUltimateAuthServer()`:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Session.Lifetime = TimeSpan.FromDays(7);
    options.Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
});
```

The configuration delegate receives:

```csharp
UAuthServerOptions
```

The server registration also supports an optional policy configuration delegate.

```csharp
builder.Services.AddUltimateAuthServer(
    configure: options =>
    {
        options.Login.MaxFailedAttempts = 5;
    },
    configurePolicies: policies =>
    {
        // Register application access policies.
    });
```

The server automatically registers its Core infrastructure. You do not need to call `AddUltimateAuth()` separately.

### Configuration from appsettings.json

UltimateAuth Server binds the configuration section:

```text
UltimateAuth:Server
```

For example:

```json
{
  "UltimateAuth": {
    "Server": {
      "Login": {
        "MaxFailedAttempts": 5,
        "LockoutDuration": "00:15:00"
      },
      "Session": {
        "Lifetime": "7.00:00:00"
      },
      "Token": {
        "AccessTokenLifetime": "00:10:00"
      }
    }
  }
}
```

ASP.NET Core represents `TimeSpan` values using strings such as `00:15:00` for fifteen minutes and `7.00:00:00` for seven days.

The configuration section is bound after the registration delegate, so configuration provider values take precedence for matching properties.

For configuration source ordering and environment-specific overrides, see [Configuration Overview](/docs/configuration/configuration).

## 2. Server Options Architecture

`UAuthServerOptions` contains two categories of configuration.

### Core-defined options

These types are defined in UltimateAuth Core and composed into the server options:

| Property | Type | Purpose |
|---|---|---|
| `Login` | `UAuthLoginOptions` | Login attempts and lockout behavior |
| `Session` | `UAuthSessionOptions` | Session lifetime and lifecycle |
| `Token` | `UAuthTokenOptions` | Access and refresh token behavior |
| `Pkce` | `UAuthPkceOptions` | PKCE authorization code lifetime |
| `MultiTenant` | `UAuthMultiTenantOptions` | Tenant identification and resolution |
| `Pagination` | `UAuthPaginationOptions` | Pagination defaults and limits |
| `Events` | `UAuthEvents` | Authentication lifecycle callbacks |

### Server-specific options

| Property | Purpose |
|---|---|
| `AllowedModes` | Restrict permitted authentication modes |
| `HubDeploymentMode` | Describe the relationship between UAuthHub and the application |
| `Hub` | Hub integration and flow settings |
| `Cookie` | Cookie names, security attributes, and lifetime behavior |
| `SessionResolution` | Session identifier sources and precedence |
| `Endpoints` | Endpoint groups and disabled actions |
| `PrimaryCredential` | Primary credential selection for UI and API requests |
| `AuthResponse` | Authentication credential delivery and redirect behavior |
| `CredentialSessionPolicy` | Session invalidation after credential changes |
| `ResetCredential` | Credential recovery code and token settings |
| `Identifiers` | Identifier behavior, normalization, and uniqueness |
| `IdentifierValidation` | Username, email, and phone validation |
| `LoginIdentifiers` | Identifier types allowed for login |
| `UserProfile` | User profile capabilities |
| `Navigation` | Authentication navigation behavior |
| `Diagnostics` | Optional diagnostic response details |

Advanced configuration hooks are also available, including `ConfigureMode()` and `OnConfigureEndpoints`.

## 3. Login Options

`options.Login` controls login attempt limits, lockout behavior, and related security settings.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Login.LockoutDuration = TimeSpan.FromMinutes(15);
    options.Login.FailureWindow = TimeSpan.FromMinutes(15);
    options.Login.ExtendLockOnFailure = false;
});
```

### Available Properties

| Property | Default | Description |
|---|---|---|
| `MaxFailedAttempts` | `10` | Failed login threshold; `0` disables lockout |
| `LockoutDuration` | 15 minutes | Lockout duration |
| `FailureWindow` | 15 minutes | Window for counting failed attempts |
| `ExtendLockOnFailure` | `false` | Extend lockout when another attempt fails during lockout |
| `IncludeFailureDetails` | `true` | Include detailed authentication failure information |
| `TryLoginDuration` | 180 seconds | Duration associated with the try-login flow |

### Example: Restrict Login Failure Details

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.IncludeFailureDetails = false;
});
```

This setting is useful when an application does not want to expose detailed lockout information to callers.

### Important

Lockout configuration is not equivalent to complete network-level rate limiting.

Production deployments should also consider infrastructure protections against abusive traffic and distributed login attempts.

## 4. Session Options

`options.Session` controls session lifetime, expiration, sliding behavior, and session-chain limits.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromDays(7);
    options.Session.SlidingExpiration = true;
    options.Session.IdleTimeout = TimeSpan.FromDays(3);
    options.Session.MaxLifetime = TimeSpan.FromDays(30);
});
```

### Session Lifetime Properties

| Property | Default | Description |
|---|---|---|
| `Lifetime` | 7 days | Standard session lifetime |
| `MaxLifetime` | `null` | Optional absolute lifetime limit |
| `SlidingExpiration` | `true` | Allow eligible activity or refresh operations to extend expiration |
| `IdleTimeout` | `null` | Optional inactivity timeout |
| `TouchInterval` | 5 minutes | Minimum interval between last-seen updates |

These settings have different purposes.

`Lifetime` determines the standard lifetime assigned to a session.

`MaxLifetime` establishes an optional hard boundary that cannot be extended indefinitely.

`IdleTimeout` limits how long a session can remain inactive.

`TouchInterval` reduces unnecessary persistence operations caused by frequent activity updates.

### Session-Chain Properties

| Property | Default | Description |
|---|---|---|
| `MaxChainsPerUser` | `0` | Reserved user-level chain limit |
| `MaxSessionsPerChain` | `100` | Session-chain rotation limit |
| `MaxChainsPerPlatform` | `null` | Reserved platform-specific chain limits |
| `PlatformCategories` | `null` | Reserved platform group definitions |
| `MaxChainsPerCategory` | `null` | Reserved category-specific chain limits |
| `EnableIpBinding` | `false` | Reserved IP binding setting |
| `EnableUserAgentBinding` | `false` | Reserved User-Agent binding setting |
| `DeviceMismatchBehavior` | `Reject` | Reserved device mismatch behavior |

**Current implementation limitation:** The source identifies several device limits and binding settings as reserved for future enforcement. Setting these properties must not be treated as proof that the corresponding protection is active.

For example, do not rely on:

```csharp
options.Session.MaxChainsPerUser = 1;
```

as a production guarantee that only one device chain can exist unless the current runtime implementation has been verified to enforce that restriction.

### Mode-Specific Session Defaults

Session settings can also be affected by authentication mode defaults.

For example, `PureOpaque` supplies a longer idle timeout and touch interval when those values have not been configured.

`Hybrid` explicitly sets `TouchInterval` to `null`.

See [Advanced Configuration](/docs/configuration/advanced-configuration) for the effective-options resolution order.

## 5. Token Options

`options.Token` configures access and refresh token behavior.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Token.Issuer = "MyApplication";
    options.Token.Audience = "MyApplicationApi";
    options.Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
    options.Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
});
```

### Available Properties

| Property | Default | Description |
|---|---|---|
| `IssueJwt` | `true` | Enable JWT issuance where supported by the effective mode |
| `IssueOpaque` | `true` | Enable opaque token issuance where supported |
| `IssueRefresh` | `true` | Enable refresh token issuance where supported |
| `AccessTokenLifetime` | 10 minutes | Access token lifetime |
| `RefreshTokenLifetime` | 7 days | Refresh token lifetime |
| `OpaqueIdBytes` | `32` | Random byte length used for opaque token identifiers |
| `RefreshTokenConcurrentRequestWindow` | 2 seconds | Concurrent duplicate refresh classification window |
| `Issuer` | `UAuth` | JWT issuer |
| `Audience` | `UAuthClient` | JWT audience |
| `AddJwtIdClaim` | `false` | Include a JWT identifier claim |
| `KeyId` | `null` | Optional signing key identifier |

### Refresh Token Concurrency Window

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromSeconds(2);
});
```

This option controls how the server distinguishes a closely timed duplicate refresh request from a confirmed replay.

A duplicate refresh request is still rejected.

The window affects the replay-response classification, including whether the token family should be revoked.

It does **not** allow the same consumed refresh token to be used successfully more than once.

### Mode Defaults May Override Token Values

The effective `Hybrid` configuration explicitly sets:

```csharp
Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
```

Therefore, a different value configured globally may be replaced when the effective mode is resolved.

To customize the lifetime for `Hybrid`, use:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
    });
});
```

This distinction is essential when configuring token lifetimes.

## 6. PKCE Options

`options.Pkce` configures the lifetime of PKCE authorization codes.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Pkce.AuthorizationCodeLifetimeSeconds = 120;
});
```

| Property | Default | Description |
|---|---|---|
| `AuthorizationCodeLifetimeSeconds` | `120` | Lifetime of a PKCE authorization code |

PKCE is particularly important for browser-based clients such as Blazor WebAssembly.

A shorter authorization code lifetime reduces the period during which an intercepted or exposed code could potentially be used.

However, the value must remain long enough for legitimate authorization exchanges to complete.

The server's PKCE configuration is separate from the client's PKCE flow settings.

## 7. Authentication Modes

UltimateAuth supports authentication mode resolution based on the client profile.

The server exposes:

```csharp
options.AllowedModes
```

This property restricts the modes the server is permitted to use.

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

`AllowedModes` is a restriction, not a mode selector.

The effective mode is resolved by `IEffectiveAuthModeResolver`.

### Default Mode Resolution

| Client profile | Default resolved mode |
|---|---|
| `BlazorServer` | `PureOpaque` |
| `WebServer` | `Hybrid` |
| `BlazorWasm` | `Hybrid` |
| `Maui` | `Hybrid` |
| `Api` | `PureJwt` |

The current implementation has mode templates for `PureOpaque`, `Hybrid`, `SemiHybrid`, and `PureJwt`.

However, `SemiHybrid` and `PureJwt` are not yet accepted as enabled modes by the current server options validator.

**Do not enable an unsupported mode merely because it exists in the enum or has a mode-default template.**

The default `Api` profile mapping to `PureJwt` is a current compatibility limitation to consider when deploying API-profile authentication flows.

### Configure Mode-Specific Behavior

Use `ConfigureMode()` for behavior that should differ by mode.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ConfigureMode(UAuthMode.PureOpaque, mode =>
    {
        mode.Session.IdleTimeout = TimeSpan.FromDays(7);
    });

    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
    });
});
```

The mode override runs after the built-in mode defaults.

See [Advanced Configuration](/docs/configuration/advanced-configuration) for details.

## 8. Endpoint Options

`options.Endpoints` controls endpoint paths, feature groups, and individual action restrictions.

### Base Path

The default server endpoint base path is:

```text
/auth
```

Configure it through:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.BasePath = "/auth";
});
```

Changing the base path can help avoid route conflicts when integrating UltimateAuth into an existing application.

The client must use compatible endpoint paths.

### Endpoint Groups

| Property | Default | Purpose |
|---|---|---|
| `Authentication` | `true` | Authentication endpoints |
| `Pkce` | `true` | PKCE endpoints |
| `Session` | `true` | Session management endpoints |
| `UserLifecycle` | `true` | User lifecycle endpoints |
| `UserProfile` | `true` | User profile endpoints |
| `UserIdentifier` | `true` | Identifier management endpoints |
| `Credentials` | `true` | Credential management endpoints |
| `Authorization` | `true` | Authorization endpoints |

For example:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.Authentication = true;
    options.Endpoints.Session = true;
    options.Endpoints.Authorization = false;
});
```

Disabling an endpoint group controls endpoint exposure. It does not automatically remove the underlying domain services or invalidate previously issued credentials.

### Disable Individual Actions

`DisabledActions` is a collection of action identifiers:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.DisabledActions.Add("users.create.anonymous");
});
```

The string must match the action identifier used by the server's endpoint registration and action policy.

Do not pass a C# constant name as a string unless that exact text is also the registered action value.

For example, these are not necessarily equivalent:

```csharp
"UAuthActions.Users.Create.Anonymous"
```

and:

```csharp
"users.create.anonymous"
```

Use the actual action value defined by the corresponding `UAuthActions` constant.

### Endpoint Hardening

Disable endpoints that are not required by the application.

For example, an application that delegates user provisioning to administrators may choose to disable anonymous user creation.

Endpoint exposure is a separate concern from authorization: an exposed endpoint must still enforce the applicable security policy.

## 9. Session Resolution

`options.SessionResolution` controls how incoming requests provide session identifiers.

UltimateAuth supports four resolution sources:

- Bearer credentials.
- Custom headers.
- Cookies.
- Query parameters.

### Default Configuration

| Property | Default |
|---|---|
| `EnableBearer` | `true` |
| `EnableHeader` | `true` |
| `EnableCookie` | `true` |
| `EnableQuery` | `true` |
| `HeaderName` | `X-UAuth-Session` |
| `QueryParameterName` | `session_id` |

The default precedence order is:

```text
Bearer
Header
Cookie
Query
```

### Configure Resolution Sources

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.SessionResolution.EnableBearer = true;
    options.SessionResolution.EnableHeader = false;
    options.SessionResolution.EnableCookie = true;
    options.SessionResolution.EnableQuery = false;
});
```

This example disables custom-header and query-string session resolution.

### Change Resolution Order

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.SessionResolution.Order =
    [
        "Cookie",
        "Bearer",
        "Header",
        "Query"
    ];
});
```

Resolution order matters when a request contains multiple candidate credentials.

The selected credential must still pass authentication and session validation.

### Security Recommendation

Avoid query-string credentials unless the application has a specific, well-understood requirement.

URLs may be recorded in browser history, reverse-proxy logs, analytics systems, and other infrastructure.

For most browser and API integrations, cookies or authorization headers are preferable.

## 10. Cookie Configuration

`options.Cookie` controls cookies used for authentication credentials.

UltimateAuth defines three cookie groups:

```csharp
options.Cookie.Session
options.Cookie.AccessToken
options.Cookie.RefreshToken
```

### Default Cookie Names

| Cookie | Default name |
|---|---|
| Session | `uas` |
| Access token | `uat` |
| Refresh token | `uar` |

The default cookie options use:

- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `Path = "/"`
- `Domain = null`
- `MaxAge = null`

`SameSite` is nullable and can be resolved through the applicable cookie and deployment policies.

### Customize Cookie Names

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Cookie.Session.Name = "myapp_session";
    options.Cookie.RefreshToken.Name = "myapp_refresh";
});
```

### Configure Cookie Scope

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Cookie.Session.Path = "/";
    options.Cookie.Session.Domain = null;
});
```

A null domain produces a host-only cookie.

Setting a parent domain can broaden cookie availability to subdomains and should be done only when necessary.

### Cookie Security

Avoid disabling `HttpOnly` or relaxing secure transport requirements for authentication cookies.

Cookie settings interact with:

- The deployment mode.
- Cross-site requests.
- Browser cookie policies.
- Credential delivery configuration.
- HTTPS requirements.

For cross-site deployments, configure cookie behavior together with CORS and the relevant browser security requirements.

## 11. Hub Deployment Mode

`options.HubDeploymentMode` describes how UAuthHub is deployed relative to the application.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.HubDeploymentMode = UAuthHubDeploymentMode.Embedded;
});
```

### Available Modes

| Mode | Description |
|---|---|
| `Embedded` | Hub and application share the same origin |
| `Integrated` | Hub is separate but within the same-site boundary |
| `External` | Hub is hosted on a different site or domain |

The default is:

```csharp
UAuthHubDeploymentMode.Integrated
```

### Embedded

Suitable for applications hosting UltimateAuth within the same application and origin.

```csharp
options.HubDeploymentMode = UAuthHubDeploymentMode.Embedded;
```

### Integrated

Suitable for deployments such as:

```text
auth.example.com
app.example.com
```

where the authentication service and application are separate origins within the same site.

### External

Suitable for a separate authentication provider domain.

```csharp
options.HubDeploymentMode = UAuthHubDeploymentMode.External;
```

External deployment requires careful coordination of cookie policies, redirects, allowed origins, and browser restrictions.

The deployment mode influences authentication transport behavior; it is not merely descriptive metadata.

## 12. Hub Options

`options.Hub` configures UAuthHub integration.

### Available Properties

| Property | Default | Description |
|---|---|---|
| `ClientBaseAddress` | `null` | Optional client application base address |
| `AllowedClientOrigins` | Empty set | Allowed client origins |
| `FlowLifetime` | 5 minutes | Lifetime of Hub flow artifacts |
| `LoginPath` | `/login` | Hub login page path |

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Hub.ClientBaseAddress = "https://app.example.com";
    options.Hub.AllowedClientOrigins.Add("https://app.example.com");
    options.Hub.FlowLifetime = TimeSpan.FromMinutes(5);
    options.Hub.LoginPath = "/login";
});
```

The Hub flow lifetime is not the same as the authenticated session lifetime.

Hub artifacts support authentication orchestration and should generally be short-lived.

Only trusted origins should be included in `AllowedClientOrigins`.

## 13. Primary Credential Policy

`options.PrimaryCredential` controls which credential kind is preferred for UI and API requests.

The defaults are:

| Property | Default |
|---|---|
| `Ui` | `PrimaryTokenKind.Session` |
| `Api` | `PrimaryTokenKind.AccessToken` |

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.PrimaryCredential.Ui = PrimaryTokenKind.Session;
    options.PrimaryCredential.Api = PrimaryTokenKind.AccessToken;
});
```

This allows UltimateAuth to distinguish between session-oriented browser interactions and access-token-oriented API interactions.

The primary credential policy does not bypass authentication validation.

It determines which credential kind should be preferred within the applicable request-processing rules.

## 14. Credential Session Policy

`options.CredentialSessionPolicy` determines what happens to active session chains after credential changes.

### Available Properties

| Property | Default | Purpose |
|---|---|---|
| `OnSelfChange` | `PreserveCurrent` | User changes their own credential |
| `OnAdminChange` | `RevokeAll` | Administrator changes a user's credential |
| `OnRecovery` | `RevokeAll` | Credential recovery completes |

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.CredentialSessionPolicy.OnSelfChange = CredentialSessionBehavior.PreserveCurrent;
    options.CredentialSessionPolicy.OnAdminChange = CredentialSessionBehavior.RevokeAll;
    options.CredentialSessionPolicy.OnRecovery = CredentialSessionBehavior.RevokeAll;
});
```

### Available Behaviors

| Behavior | Effect |
|---|---|
| `PreserveAll` | Keep existing session chains |
| `PreserveCurrent` | Keep the current chain and revoke other applicable chains |
| `RevokeAll` | Revoke all applicable session chains |

For recovery flows, `PreserveCurrent` is not always meaningful because the user may not have an authenticated current session.

The current implementation treats this situation using the more restrictive `RevokeAll` behavior.

### Security Recommendation

Preserving the current session after a normal password change can provide a reasonable user experience.

Revoking all sessions after administrative credential changes or account recovery is generally more conservative.

Choose the policy according to the application's security requirements.

## 15. Credential Reset Options

`options.ResetCredential` configures credential recovery proof lifetimes and attempt limits.

### Defaults

| Property | Default | Description |
|---|---|---|
| `TokenValidity` | 30 minutes | Reset token lifetime |
| `CodeValidity` | 10 minutes | Numeric reset code lifetime |
| `MaxAttempts` | `3` | Maximum permitted recovery attempts |
| `CodeLength` | `6` | Numeric reset code length |

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.ResetCredential.TokenValidity = TimeSpan.FromMinutes(20);
    options.ResetCredential.CodeValidity = TimeSpan.FromMinutes(5);
    options.ResetCredential.MaxAttempts = 3;
    options.ResetCredential.CodeLength = 6;
});
```

`CodeLength` applies to numeric reset codes, not token-based recovery proofs.

### Important Security Boundary

Reset proofs must be delivered through a trusted server-side mechanism.

Do not return recovery tokens to anonymous callers simply to simplify a client-side reset form.

The application should provide the appropriate `ICredentialResetNotifier` implementation for supported recovery flows.

## 16. Login Identifier Options

`options.LoginIdentifiers` determines which identifier types can be used for login.

The default allowed types are:

```csharp
UserIdentifierType.Username
UserIdentifierType.Email
UserIdentifierType.Phone
```

### Restrict Login Identifier Types

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.LoginIdentifiers.AllowedTypes =
        new HashSet<UserIdentifierType>
        {
            UserIdentifierType.Email
        };
});
```

This configures email as the allowed login identifier type.

### Require Verification

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.LoginIdentifiers.RequireVerificationForEmail = true;
    options.LoginIdentifiers.RequireVerificationForPhone = true;
});
```

These settings control whether the corresponding identifiers must be verified before being accepted for login.

### Custom Resolvers

| Property | Default |
|---|---|
| `EnableCustomResolvers` | `true` |
| `CustomResolversFirst` | `true` |

These options control the participation and ordering of custom login identifier resolvers.

An identifier existing on a user account does not necessarily mean it is permitted for login.

Identifier management and login eligibility are separate concerns.

## 17. Identifier Validation

`options.IdentifierValidation` provides rules for usernames, email addresses, and phone numbers.

### Username Rules

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.IdentifierValidation.UserName.Enabled = true;
    options.IdentifierValidation.UserName.MinLength = 3;
    options.IdentifierValidation.UserName.MaxLength = 32;
    options.IdentifierValidation.UserName.AllowedRegex = "^[a-zA-Z0-9._-]+$";
});
```

### Default Validation Rules

| Identifier | Enabled | Minimum length | Maximum length |
|---|---|---|---|
| Username | `true` | `3` | `64` |
| Email | `true` | `3` | `256` |
| Phone | `true` | `3` | `24` |

Username validation additionally supports `AllowedRegex`.

The default username expression is:

```regex
^[a-zA-Z0-9._-]+$
```

### Identifier Behavior

`options.Identifiers` contains additional groups:

```csharp
options.Identifiers.Behavior
options.Identifiers.Validation
options.Identifiers.Normalization
options.Identifiers.Uniqueness
```

These configure identifier behavior beyond the top-level validation rules.

Identifier normalization and uniqueness are particularly important because they affect how duplicate identifiers are detected.

Use the provided options rather than implementing inconsistent normalization independently in the client.

### Identifier Limits

UltimateAuth supports configurable limits on the number of identifiers associated with each user.

By default, a user can have up to **10 active identifiers**, regardless of their type.

```csharp
options.Identifiers.Behavior.MaxIdentifiersPerUser = 10;
```

This is a global per-user limit. It applies to the combined number of usernames, email addresses, phone numbers, and custom identifiers.

#### Optional Per-Type Limits

Applications can optionally define limits for individual identifier types:

```csharp
options.Identifiers.Behavior.MaxIdentifiersPerUser = 10;

options.Identifiers.Behavior.MaxIdentifiersPerType = new()
{
    [UserIdentifierType.Username] = 2,
    [UserIdentifierType.Email] = 5,
    [UserIdentifierType.Phone] = 3
};
```

**Per-type limits are not configured by default.**

When no per-type limit is specified, only the global limit applies.

For example, with the configuration above, a user may have up to five email addresses, but the combined number of all active identifiers cannot exceed ten.

#### Default Behavior

| Option | Default | Description |
|---|---|---|
| `MaxIdentifiersPerUser` | `10` | Maximum total active identifiers per user |
| `MaxIdentifiersPerType` | Empty dictionary | Optional limits for individual identifier types |

The following rules apply:

- Primary and non-primary identifiers count equally.
- Soft-deleted identifiers do not count toward active limits.
- Self-service and administrative operations follow the same limits.
- Per-type limits do not override the global limit.
- Existing identifier behavior options remain applicable.

For example, `AllowMultipleUsernames = false` still prevents multiple usernames, even when the configured username limit is greater than one.

#### Limit Validation

When an identifier addition exceeds a configured limit, the operation is rejected with a validation error.

| Error Code | Meaning |
|---|---|
| `identifier_total_limit_exceeded` | The user has reached the global identifier limit |
| `identifier_type_limit_exceeded` | The user has reached the configured limit for that identifier type |

Applications should handle these errors and display appropriate feedback.

Identifier limits are enforced by the Users application layer rather than individual persistence stores.

## 18. User Profile Options

`options.UserProfile` controls profile-related capabilities.

The current option is:

```csharp
options.UserProfile.EnableMultiProfile
```

Its default is `false`.

### Enable Multiple Profiles

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.UserProfile.EnableMultiProfile = true;
});
```

This allows applications to use UltimateAuth's multiple-profile capability where supported by the user domain.

For example, an application may maintain separate profile contexts for different purposes.

Multi-profile support does not create separate user identities or independent authentication accounts.

## 19. Multi-Tenant Options

`options.MultiTenant` controls whether tenant-aware behavior is enabled and how tenant identifiers are resolved.

### Available Properties

| Property | Default | Description |
|---|---|---|
| `Enabled` | `false` | Enable multi-tenant behavior |
| `NormalizeToLowercase` | `true` | Normalize tenant identifiers |
| `EnableRoute` | `false` | Resolve tenants from routes |
| `EnableHeader` | `false` | Resolve tenants from headers |
| `EnableDomain` | `false` | Resolve tenants from hosts |
| `HeaderName` | `X-Tenant` | Tenant header name |

### Example: Header-Based Tenant Resolution

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.MultiTenant.Enabled = true;
    options.MultiTenant.EnableHeader = true;
    options.MultiTenant.HeaderName = "X-Tenant";
});
```

### Example: Domain-Based Resolution

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.MultiTenant.Enabled = true;
    options.MultiTenant.EnableDomain = true;
});
```

Multiple resolution mechanisms can be enabled.

The server composes the configured tenant resolvers according to its resolution infrastructure.

### Security Boundary

Tenant identification is not tenant authorization.

A request providing another tenant identifier must not gain access to that tenant without appropriate authorization.

The server remains responsible for tenant isolation.

## 20. Pagination Options

`options.Pagination` controls pagination defaults and maximum page sizes.

| Property | Default |
|---|---|
| `DefaultPageSize` | `250` |
| `MaxPageSize` | `1000` |

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Pagination.DefaultPageSize = 50;
    options.Pagination.MaxPageSize = 200;
});
```

These settings affect paginated queries exposed by UltimateAuth domain services.

For administrative interfaces, choosing a moderate page size can reduce unnecessary data transfer and processing.

## 21. Navigation Options

`options.Navigation` configures automatic authentication navigation behavior.

### Available Properties

| Property | Default |
|---|---|
| `EnableAutomaticNavigationRedirect` | `true` |
| `LoginResolver` | `null` |
| `AccessDeniedResolver` | `null` |

### Disable Automatic Redirects

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Navigation.EnableAutomaticNavigationRedirect = false;
});
```

### Custom Login Redirect

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Navigation.LoginResolver = context =>
    {
        return "/account/login";
    };
});
```

### Custom Access Denied Redirect

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Navigation.AccessDeniedResolver = context =>
    {
        return "/account/access-denied";
    };
});
```

These delegates receive the current `HttpContext`.

The application should return valid, trusted navigation destinations.

Avoid constructing redirects from unvalidated user-controlled URLs.

## 22. Authentication Response Options

`options.AuthResponse` controls how authentication credentials and redirect behavior are represented in responses.

The available groups are:

```csharp
options.AuthResponse.SessionIdDelivery
options.AuthResponse.AccessTokenDelivery
options.AuthResponse.RefreshTokenDelivery

options.AuthResponse.Login
options.AuthResponse.Logout
```

These settings govern credential delivery and authentication response behavior.

### Important: Mode-Specific Response Defaults

UltimateAuth provides response templates for authentication modes.

For example, `PureOpaque` uses a session-oriented cookie response, while `Hybrid` combines session and token delivery behavior.

The effective response may therefore depend on the resolved authentication mode and flow.

Do not assume that a single globally configured response delivery setting applies unchanged to every authentication mode.

### Security Considerations

Credential delivery settings determine whether sensitive authentication material is exposed through cookies, headers, or other response mechanisms.

Only customize them when the application has a clear requirement and the selected transport is appropriate for the client platform.

For advanced response customization, see [Advanced Configuration](/docs/configuration/advanced-configuration).

## 23. Diagnostics Options

`options.Diagnostics` provides optional diagnostic behavior.

The current property is:

```csharp
options.Diagnostics.EnableRefreshDetails
```

Its default is `false`.

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Diagnostics.EnableRefreshDetails = false;
});
```

When enabled, the option allows additional refresh-related diagnostic response information, such as the `X-UAuth-Refresh` header.

This is primarily useful during development and debugging.

Avoid enabling detailed authentication diagnostics in production unless the exposure is understood and justified.

## 24. Authentication Lifecycle Events

`options.Events` exposes server-side authentication lifecycle callbacks.

### Available Events

```csharp
options.Events.OnAnyEvent
options.Events.OnUserLoggedIn
options.Events.OnUserLoggedOut
```

### Example: Login Logging

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Events.OnUserLoggedIn = context =>
    {
        // Record the successful login.
        return Task.CompletedTask;
    };
});
```

### Example: Global Event Handler

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Events.OnAnyEvent = context =>
    {
        // Process an authentication lifecycle event.
        return Task.CompletedTask;
    };
});
```

Lifecycle events are intended for lightweight reactions such as logging, metrics, and notifications.

They should not replace security decisions, authentication validation, or application policies.

See [Advanced Configuration](/docs/configuration/advanced-configuration) for event and extension guidance.

## 25. Effective Server Options

The configured `UAuthServerOptions` instance is the base configuration.

At runtime, UltimateAuth can resolve:

```csharp
EffectiveUAuthServerOptions
```

The current resolution process is:

```text
Configured UAuthServerOptions
            |
            v
      Resolve auth mode
            |
            v
      Clone base options
            |
            v
      Apply mode defaults
            |
            v
  Apply ConfigureMode overrides
            |
            v
  EffectiveUAuthServerOptions
```

This means that the effective settings used for a `Hybrid` client can differ from those used for a `PureOpaque` client.

### Example

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.FromDays(7);

    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
    });

    options.ConfigureMode(UAuthMode.PureOpaque, mode =>
    {
        mode.Session.IdleTimeout = TimeSpan.FromDays(3);
    });
});
```

The original options remain the common starting point.

Mode defaults and overrides determine the effective values for the resolved mode.

For implementation details and inspection of effective options, see [Advanced Configuration](/docs/configuration/advanced-configuration).

## 26. Validation and Configuration Safety

UltimateAuth Server registers multiple options validators and enables startup validation.

These validators cover areas including:

- Login settings.
- Session settings.
- Token settings.
- PKCE.
- Multi-tenancy.
- Identifier configuration.
- Session resolution.
- Pagination.
- General server configuration.

### Example: Invalid Session Lifetime

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Session.Lifetime = TimeSpan.Zero;
});
```

The server rejects an invalid session lifetime under the applicable validation rules.

### Example: Invalid Base Path

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Endpoints.BasePath = "//auth";
});
```

The general server validator rejects paths containing `//`.

### Unsupported Modes

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.AllowedModes =
    [
        UAuthMode.PureJwt
    ];
});
```

The current validator rejects explicitly enabling `PureJwt` because that mode is not yet implemented as a supported server mode.

### Validation Limitations

Startup validation protects the configured options according to the registered validators.

It does not guarantee that every custom service implementation or mode-specific override is secure.

When using advanced customization, test the resulting effective configuration and authentication flows.

## 27. Complete Configuration Example

The following example combines several commonly used server settings.

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    // Authentication modes
    options.AllowedModes =
    [
        UAuthMode.PureOpaque,
        UAuthMode.Hybrid
    ];

    // Login security
    options.Login.MaxFailedAttempts = 5;
    options.Login.LockoutDuration = TimeSpan.FromMinutes(15);
    options.Login.IncludeFailureDetails = false;

    // Sessions
    options.Session.Lifetime = TimeSpan.FromDays(7);
    options.Session.MaxLifetime = TimeSpan.FromDays(30);

    // Token identity
    options.Token.Issuer = "MyApplication";
    options.Token.Audience = "MyApplicationApi";

    // Hybrid-specific token lifetime
    options.ConfigureMode(UAuthMode.Hybrid, mode =>
    {
        mode.Token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
    });

    // PKCE
    options.Pkce.AuthorizationCodeLifetimeSeconds = 120;

    // Hub deployment
    options.HubDeploymentMode = UAuthHubDeploymentMode.Integrated;
    options.Hub.ClientBaseAddress = "https://app.example.com";
    options.Hub.AllowedClientOrigins.Add("https://app.example.com");

    // Credential security
    options.CredentialSessionPolicy.OnSelfChange = CredentialSessionBehavior.PreserveCurrent;
    options.CredentialSessionPolicy.OnAdminChange = CredentialSessionBehavior.RevokeAll;
    options.CredentialSessionPolicy.OnRecovery = CredentialSessionBehavior.RevokeAll;

    // Credential recovery
    options.ResetCredential.TokenValidity = TimeSpan.FromMinutes(30);
    options.ResetCredential.MaxAttempts = 3;

    // Identifier login rules
    options.LoginIdentifiers.RequireVerificationForEmail = true;

    // Session transport
    options.SessionResolution.EnableCookie = true;
    options.SessionResolution.EnableBearer = true;
    options.SessionResolution.EnableQuery = false;

    // Endpoint exposure
    options.Endpoints.Authentication = true;
    options.Endpoints.Session = true;
    options.Endpoints.Credentials = true;
    options.Endpoints.Authorization = true;

    // Pagination
    options.Pagination.DefaultPageSize = 50;
    options.Pagination.MaxPageSize = 200;

    // Diagnostics
    options.Diagnostics.EnableRefreshDetails = false;
});
```

This is an illustrative configuration, not a universal production preset.

Applications should choose session lifetimes, authentication modes, credential transport, tenant behavior, and endpoint exposure according to their own deployment and security requirements.

## 28. Common Configuration Mistakes

### Configuring Core Options Separately

When using `AddUltimateAuthServer()`, configure Core-defined options through `UAuthServerOptions`.

A separate `UAuthOptions` configuration does not automatically override the server's composed options.

### Assuming AllowedModes Selects the Mode

`AllowedModes` is a restriction.

Mode selection is performed by the authentication mode resolver.

### Ignoring Mode Defaults

Mode-specific defaults can replace globally configured values.

Use `ConfigureMode()` for explicit mode-specific behavior.

### Enabling Unsupported Modes

An authentication mode appearing in an enum does not guarantee runtime support.

### Treating Reserved Session Options as Enforced Security Features

Some device and session-chain options are reserved for future enforcement.

Verify actual runtime support before relying on them.

### Exposing Unnecessary Endpoints

Disable endpoint groups or actions that the application does not require.

### Allowing Query-String Credentials Without a Specific Need

Query-string credentials can leak through URLs and infrastructure logs.

### Weakening Cookie Security

Do not relax secure transport or cookie protections without understanding the consequences.

### Assuming Configuration Validation Covers Everything

Startup validation does not replace testing, infrastructure security, or validation of custom extension implementations.

## 29. Server Options Quick Reference

| Configuration group | Typical use |
|---|---|
| `Login` | Login attempts and lockout |
| `Session` | Session expiration and lifecycle |
| `Token` | Token issuance and lifetime |
| `Pkce` | PKCE code lifetime |
| `AllowedModes` | Permitted authentication modes |
| `MultiTenant` | Tenant resolution |
| `Pagination` | Query page limits |
| `Endpoints` | Endpoint exposure |
| `SessionResolution` | Credential source precedence |
| `Cookie` | Cookie security and scope |
| `HubDeploymentMode` | Hub deployment topology |
| `Hub` | Hub integration |
| `PrimaryCredential` | UI/API primary credential selection |
| `AuthResponse` | Credential delivery |
| `CredentialSessionPolicy` | Session invalidation after credential changes |
| `ResetCredential` | Credential recovery rules |
| `Identifiers` | Identifier behavior |
| `IdentifierValidation` | Identifier validation |
| `LoginIdentifiers` | Login identifier eligibility |
| `UserProfile` | Multi-profile capability |
| `Navigation` | Authentication redirects |
| `Diagnostics` | Refresh diagnostics |
| `Events` | Authentication lifecycle callbacks |
| `ConfigureMode()` | Mode-specific option overrides |
| `OnConfigureEndpoints` | Advanced endpoint customization |

## Next Steps

- [Configuration Overview](/docs/configuration/configuration) — Understand the configuration architecture and precedence.
- [Client Options](/docs/configuration/client-options) — Configure client SDK behavior.
- [Advanced Configuration](/docs/configuration/advanced-configuration) — Explore effective options, mode resolution, and extension points.
- [Session Management](/docs/client/session-management) — Manage sessions through the client.
- [Authentication](/docs/client/authentication) — Use the client authentication flows.