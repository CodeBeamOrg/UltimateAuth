---
title: Device Limits
order: 3
group: sessions-devices
---

# Device Limits

UltimateAuth supports **opt-in, server-enforced limits on distinct active devices per user and tenant**. Configure an overall limit, per-device-type limits, or both.

Device Limits applies when a server-managed authentication session is issued. It does not limit browser tabs, count every historical session chain, or restrict the total number of sessions ever created.

## Configuration

Configure device limits through `UAuthServerOptions`:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.DeviceLimits.MaxDevices = 3;
    options.DeviceLimits.TypeLimits["web"] = 2;
    options.DeviceLimits.Behavior = DeviceLimitBehavior.RejectNewLogin;
});
```

`DeviceLimitBehavior` is defined in `CodeBeam.UltimateAuth.Server.Options`.

With this configuration, a user may have three active devices in total, but no more than two of them may be classified as `web`.

### Available Options

| Option | Default | Description |
|---|---|---|
| `DeviceLimits.MaxDevices` | `null` | Maximum distinct active device IDs per user and tenant |
| `DeviceLimits.TypeLimits` | Empty dictionary | Optional limits for individual device types |
| `DeviceLimits.Behavior` | `RejectNewLogin` | Determines how a new device is handled when a limit is reached |

**Device Limits is disabled by default.**

You can configure:

- Only a global limit.
- Only per-type limits.
- Both global and per-type limits.

When both are configured, admission must satisfy both restrictions.

Type names are case-insensitive. Configured limits must be positive integers, and type names cannot be empty or contain surrounding whitespace.

Invalid configurations are rejected by server options validation.

### Configuration from appsettings.json

Device limits can also be configured through the `UltimateAuth:Server` configuration section:

```json
{
  "UltimateAuth": {
    "Server": {
      "DeviceLimits": {
        "MaxDevices": 3,
        "TypeLimits": {
          "web": 2
        },
        "Behavior": "RejectNewLogin"
      }
    }
  }
}
```

## Understanding Device Counting

UltimateAuth counts **distinct active device identities**, not session chains.

A device is identified by its `DeviceId`.

A device occupies one slot when at least one qualifying active session chain is associated with that identifier.

A qualifying chain must:

- Belong to the user's active session root.
- Be active and not revoked.
- Have an active session.
- Satisfy the relevant session and security-state checks.

Expired, revoked, or otherwise inactive chains do not occupy active device slots.

### Multiple Chains on the Same Device

A single device may have multiple active session chains.

For example:

```text
User
 |
 +-- Device A (DeviceId: abc)
 |    |
 |    +-- Chain 1
 |    +-- Chain 2
 |
 +-- Device B (DeviceId: xyz)
      |
      +-- Chain 3
```

Although three chains exist, UltimateAuth counts only **two distinct devices**.

Repeated authentication from an already admitted `DeviceId` does not consume another device slot merely because a new chain is created.

### Global and Per-Type Limits

Consider:

```csharp
options.DeviceLimits.MaxDevices = 3;
options.DeviceLimits.TypeLimits["web"] = 2;
```

The following examples assume `RejectNewLogin` behavior:

| Active Devices | Incoming Device | Result |
|---|---|---|
| 1 web | New web | Allowed |
| 2 web | New web | Rejected |
| 2 web | New mobile application | Allowed |
| 2 web + 1 mobile | New device | Rejected |
| 2 web | Existing admitted web `DeviceId` | Allowed without an additional slot |

The global limit includes every device type.

A per-type limit applies only to devices with the matching classification.

## Device Limit Behaviors

UltimateAuth supports two admission behaviors.

### RejectNewLogin

This is the default behavior.

```csharp
options.DeviceLimits.Behavior = DeviceLimitBehavior.RejectNewLogin;
```

When a new device would exceed a configured limit, authentication session issuance is rejected.

Existing active devices remain unaffected.

This is useful for applications that want users to manage their registered environments explicitly.

The underlying failure code is:

```text
device_limit_exceeded
```

The login flow maps this to:

```csharp
AuthFailureReason.DeviceLimitExceeded
```

PKCE Hub failures can also use:

```csharp
HubErrorCode.DeviceLimitExceeded
```

Applications may display a message such as:

> Device limit reached. Sign out from another device before signing in.

**Authentication failure disclosure:** UltimateAuth may intentionally hide detailed failure reasons when `Login.IncludeFailureDetails` is `false`.

Do not enable detailed public authentication failures solely to customize the message without considering account enumeration risks.

### RevokeOldest

```csharp
options.DeviceLimits.Behavior = DeviceLimitBehavior.RevokeOldest;
```

When a new device would exceed a configured limit, UltimateAuth revokes eligible older devices to free capacity.

The selection rules are:

1. If a per-type limit is exceeded, the oldest eligible active device of that type is selected.
2. Otherwise, the oldest eligible active device is selected to satisfy the global limit.
3. If multiple active chains share the selected `DeviceId`, the corresponding chains are revoked together.

This process continues until the incoming device can be admitted within the configured limits.

For example, with a global limit of two:

```text
Before login:

Device A (oldest)   Active
Device B            Active

New login:

Device C            Incoming
```

With `RevokeOldest`, Device A is revoked and Device C can be admitted.

This behavior may sign users out of existing devices without an additional confirmation prompt.

Choose it only when automatic device replacement matches the application's intended user experience.

## Device Types

UltimateAuth supports device-type classifications such as:

| Type | Typical Client |
|---|---|
| `web` | Browser-based applications |
| `mobile` | Native mobile applications |
| `desktop` | Native desktop applications |
| Custom | Application-defined client types |

These are classifications, not verified hardware identities.

### Browser Classification

Built-in Blazor web clients use:

```csharp
DeviceType = "web"
```

This applies to both desktop and mobile browsers.

For example:

| Environment | DeviceType |
|---|---|
| Chrome on Windows | `web` |
| Edge on Windows | `web` |
| Safari on macOS | `web` |
| Chrome on Android | `web` |
| Safari on iOS | `web` |

The operating system and browser remain available as separate metadata.

A native desktop application may still report `desktop`, and a native mobile application may report `mobile`.

**Important:** The classification is based on the client environment, not simply the underlying operating system.

### Explicit Device Types

Custom clients may provide a device type through the request header:

```http
X-Device-Type: mobile
```

Or through the reserved form field:

```text
__uauth_device_type
```

When no valid explicit device type is supplied, server device resolution defaults to:

```text
web
```

Supplied types are normalized by trimming whitespace and converting to lowercase.

Device-type metadata must not be treated as proof of hardware identity or device ownership.

## Existing Device Classification

When an incoming `DeviceId` already has an active chain, UltimateAuth preserves its existing device-type classification.

For example:

```text
Existing active chain:

DeviceId: abc
DeviceType: desktop
```

If the same device authenticates using the updated browser client, the existing active classification is preserved rather than automatically changing to `web`.

This prevents a previously admitted device from silently moving between quota categories during subsequent authentication.

Consequently, legacy `desktop` records and newly classified `web` records may coexist.

See [Migrating to v0.2.0](/docs/migration/v0.2.0) for migration guidance.

## Device Limits vs. Session Limits

Device Limits and traditional session configuration solve different problems.

### Device Limits

```csharp
options.DeviceLimits.MaxDevices = 3;
```

Restricts the number of distinct active device identities.

### Sessions Per Chain

```csharp
options.Session.MaxSessionsPerChain = 100;
```

Controls the number of retained sessions within a session chain.

These settings are independent.

Older options such as:

- `Session.MaxChainsPerUser`
- `Session.MaxChainsPerPlatform`
- `Session.MaxChainsPerCategory`

remain reserved and must not be relied upon for enforced active-device admission.

Use `options.DeviceLimits` for device quotas.

## Persistence and Concurrency

Device admission is a server-authoritative operation.

Enforcement must remain correct when multiple authentication requests occur concurrently.

UltimateAuth introduces the following session-store contract:

```csharp
Task<TResult> ExecuteAdmissionAsync<TResult>(
    Func<CancellationToken, Task<TResult>> action,
    CancellationToken ct = default);
```

Built-in persistence implementations support this contract.

### In-Memory

The in-memory session store provides transactional admission through its existing synchronized execution mechanism.

### Entity Framework Core

The EF Core session store uses a serializable database transaction for admission.

This allows device counting, optional revocation, and session creation to participate in a coordinated transaction.

### Custom Session Stores

Custom implementations of `ISessionStore` must implement `ExecuteAdmissionAsync`.

When device limits are enabled, the implementation must provide:

- Atomic execution.
- Rollback on failure.
- Concurrency safety across application instances sharing persistence.
- Protection against simultaneous admissions exceeding the configured limits.

A simple read/count/write sequence without an appropriate concurrency mechanism is insufficient.

**Device Limits does not require an additional database table or schema migration by itself.**

Other database changes introduced in v0.2.0 may still require migrations.

## Security Considerations

### DeviceId Is Not Hardware Authentication

A `DeviceId` identifies a client-associated environment.

It is not cryptographic proof of physical device ownership.

Browser storage may be cleared, causing a new device identifier to be generated.

Likewise, a device identifier may be copied or supplied by a modified client.

Device limits should not be confused with cryptographic device binding.

### Device Type Is Descriptive

A client-reported device type is not independently trusted.

Applications should not assume that `web`, `desktop`, or `mobile` values prove the actual client hardware or operating environment.

### Device Limits Are Not Authentication

Device limits complement existing authentication and session controls.

They do not replace:

- Credential verification.
- Session validation.
- Device binding requirements.
- Authorization.
- Rate limiting.
- Session revocation.

### Provide Device Management

When device limits are enabled, applications should provide an accessible way to inspect and revoke active device sessions.

This is particularly important with `RejectNewLogin`, because a user may need to free a device slot before signing in from a new environment.

See [Device Management](/docs/sessions-devices/device-management).

## Related Documentation

- [Device Management](/docs/sessions-devices/device-management)
- [Session Model & Lifecycle](/docs/sessions-devices/session-lifecycle)
- [Session Management](/docs/client-sdk/session-management)
- [Server Options](/docs/configuration/server-options)
- [Migrating to v0.2.0](/docs/migration/v0.2.0)
