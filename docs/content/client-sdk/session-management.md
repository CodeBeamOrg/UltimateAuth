---
title: Session Management
order: 3
group: client
---

# Session Management

UltimateAuth provides a dedicated client API for inspecting and managing authentication sessions across devices and application instances.

Through `UAuthClient.Sessions`, applications can list session chains, inspect authentication history, revoke individual chains, terminate sessions across devices, and perform administrative session management.

This guide demonstrates the available operations using the current `ISessionClient` contract.

## 1. Understanding the Session Hierarchy

In session-managed authentication modes, UltimateAuth organizes authentication state into three levels:

```text
Session Root
    |
    +-- Session Chain A (Browser)
    |       |
    |       +-- Session A1
    |       +-- Session A2
    |
    +-- Session Chain B (Mobile)
            |
            +-- Session B1
```

| Level | Responsibility |
|---|---|
| **Root** | Account-level session authority |
| **Chain** | Device-associated authentication continuity |
| **Session** | Individual authentication instance |

These levels provide different boundaries for session management and revocation.

A chain is associated with a device or client context, but it is not cryptographic proof of physical device identity.

The available hierarchy and enforcement behavior depend on the authentication mode.

For architectural details, see [Session Model & Lifecycle](/docs/sessions-devices/session-lifecycle).

## 2. Accessing the Session Client

Inject `IUAuthClient` into your Blazor component:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Session operations are available through:

```csharp
UAuthClient.Sessions
```

The `Sessions` property exposes `ISessionClient`.

The interface separates operations for the current user from administrative operations targeting another user.

## 3. List My Devices and Session Chains

Use `GetMyChainsAsync` to retrieve the current user's session chains.

```csharp
var result = await UAuthClient.Sessions.GetMyChainsAsync();

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var chain in page.Items)
    {
        Console.WriteLine($"{chain.Browser} - {chain.OperatingSystem}");
    }
}
```

The method returns:

```csharp
UAuthResult<PagedResult<SessionChainSummary>>
```

The response contains a paginated collection of session chain summaries.

### SessionChainSummary

Each summary provides information suitable for building a device management interface.

| Property | Type | Description |
|---|---|---|
| `ChainId` | `SessionChainId` | Unique chain identifier |
| `DeviceType` | `string?` | Device category |
| `OperatingSystem` | `string?` | Reported operating system |
| `Platform` | `string?` | Client platform |
| `Browser` | `string?` | Reported browser |
| `CreatedAt` | `DateTimeOffset` | Chain creation time |
| `LastSeenAt` | `DateTimeOffset?` | Last recorded activity |
| `RotationCount` | `int` | Recorded rotation count |
| `TouchCount` | `int` | Recorded touch count |
| `SessionCount` | `int` | Total number of session records in the chain, including historical and revoked sessions |
| `IsRevoked` | `bool` | Whether the chain is revoked |
| `RevokedAt` | `DateTimeOffset?` | Revocation timestamp |
| `ActiveSessionId` | `AuthSessionId?` | Associated active session identifier |
| `IsCurrentDevice` | `bool` | Whether this is the current device context |
| `State` | `SessionChainState` | Current chain state |

Device metadata is descriptive information, not a trusted physical device identifier.

### Identify the Current Device

Use `IsCurrentDevice` to distinguish the current chain in your UI.

```csharp
var result = await UAuthClient.Sessions.GetMyChainsAsync();

if (result.IsSuccess && result.Value is { } page)
{
    var current = page.Items.FirstOrDefault(x => x.IsCurrentDevice);

    if (current is not null)
    {
        Console.WriteLine($"Current chain: {current.ChainId}");
    }
}
```

This is useful when displaying a device list with a **Current device** indicator.

The property is determined by the server's current authentication context.

## 4. Pagination

`GetMyChainsAsync` accepts an optional `PageRequest`.

```csharp
var result = await UAuthClient.Sessions.GetMyChainsAsync(
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 10,
        SortBy = "CreatedAt",
        Descending = true
    });
```

`PageRequest` supports:

| Property | Default | Description |
|---|---|---|
| `PageNumber` | `1` | Requested page |
| `PageSize` | `0` | Requested page size; zero uses the server pagination default |
| `SortBy` | `null` | Optional sorting field |
| `Descending` | `false` | Descending sort order |

The server applies its pagination policy, including default and maximum page sizes.

Supported sorting fields depend on the server endpoint.

### Navigating Pages

```csharp
var pageNumber = 1;

var result = await UAuthClient.Sessions.GetMyChainsAsync(
    new PageRequest
    {
        PageNumber = pageNumber,
        PageSize = 10
    });

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var chain in page.Items)
    {
        // Render the chain.
    }

    if (page.HasNext)
    {
        // Enable the next-page action.
    }
}
```

`PagedResult<T>` includes `TotalCount`, `PageNumber`, `PageSize`, and `HasNext`.

You can use these properties directly when building pagination controls.

## 5. Inspect a Session Chain

Use `GetMyChainDetailAsync` to retrieve detailed information about a specific chain.

```csharp
SessionChainId chainId = selectedChain.ChainId;

var result = await UAuthClient.Sessions.GetMyChainDetailAsync(chainId);

if (result.IsSuccess && result.Value is { } detail)
{
    Console.WriteLine($"State: {detail.State}");
    Console.WriteLine($"Created: {detail.CreatedAt}");
    Console.WriteLine($"Last seen: {detail.LastSeenAt}");
}
```

The method returns:

```csharp
UAuthResult<SessionChainDetail>
```

### SessionChainDetail

The detail model includes:

- Chain identity and device metadata.
- Creation and last-seen timestamps.
- Chain state and revocation information.
- Rotation and touch counters.
- Active session identifier.
- Session history.

### Inspect Session History

```csharp
var result = await UAuthClient.Sessions.GetMyChainDetailAsync(chainId);

if (result.IsSuccess && result.Value is { } detail)
{
    foreach (var session in detail.Sessions)
    {
        Console.WriteLine($"Session: {session.SessionId}");
        Console.WriteLine($"Created: {session.CreatedAt}");
        Console.WriteLine($"Expires: {session.ExpiresAt}");
        Console.WriteLine($"Revoked: {session.IsRevoked}");
    }
}
```

Each session is represented by `SessionInfo`.

```csharp
public sealed record SessionInfo(
    AuthSessionId SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsRevoked);
```

This allows applications to display individual authentication instances within a device-associated chain.

## 6. Understanding Chain States

`SessionChainState` defines three values:

| State | Meaning |
|---|---|
| `Active` | Active chain state |
| `Passive` | Passive chain state |
| `Revoked` | Revoked chain state |

For example:

```csharp
switch (chain.State)
{
    case SessionChainState.Active:
        // Display as active.
        break;

    case SessionChainState.Passive:
        // Display as passive.
        break;

    case SessionChainState.Revoked:
        // Display as revoked.
        break;
}
```

A passive chain should not automatically be presented as a revoked chain.

Use the returned state rather than inferring revocation from the absence of an active session.

## 7. Revoke One of My Devices

Use `RevokeMyChainAsync` to revoke a selected session chain.

```csharp
var result = await UAuthClient.Sessions.RevokeMyChainAsync(chainId);

if (result.IsSuccess)
{
    // Refresh the device list.
}
```

The method returns:

```csharp
UAuthResult<RevokeResult>
```

Revoking a chain invalidates its associated session continuity according to the server's revocation model.

It does not physically disable the device or prevent the user from authenticating again.

### Detect Revocation of the Current Chain

`RevokeResult` contains:

```csharp
public sealed record RevokeResult
{
    public bool CurrentChain { get; init; }
    public bool RootRevoked { get; init; }
}
```

You can inspect these properties:

```csharp
var result = await UAuthClient.Sessions.RevokeMyChainAsync(chainId);

if (result.IsSuccess && result.Value is { } revoke)
{
    if (revoke.CurrentChain)
    {
        // The current authentication chain was affected.
    }

    if (revoke.RootRevoked)
    {
        // The result reports root-level revocation.
    }
}
```

When `RevokeMyChainAsync` succeeds and `CurrentChain` is true, the SDK publishes a `SessionRevoked` state event.

This helps the application react when its own authentication context has been invalidated.

## 8. Revoke All Other Devices

Use `RevokeMyOtherChainsAsync` to revoke other session chains while retaining the current chain.

```csharp
var result = await UAuthClient.Sessions.RevokeMyOtherChainsAsync();

if (result.IsSuccess)
{
    // Other device chains were revoked.
}
```

This is suitable for a **Log out other devices** action in account settings.

The server determines the current chain and applies the corresponding revocation scope.

Applications should not attempt to identify the current device by comparing browser names, operating systems, or other descriptive metadata.

## 9. Revoke All My Devices

Use `RevokeAllMyChainsAsync` to revoke every session chain associated with the current user.

```csharp
var result = await UAuthClient.Sessions.RevokeAllMyChainsAsync();

if (result.IsSuccess)
{
    // All applicable chains were revoked.
}
```

This includes the current device-associated chain.

When the operation succeeds, the SDK publishes a `SessionRevoked` event.

A typical use case is an account security page offering **Log out everywhere**.

### Security Considerations

Revoking server-managed sessions prevents their continued use where the authentication mode enforces that state.

However, previously issued JWT access tokens may remain valid until expiration if the receiving resource server validates them locally without consulting current revocation state.

Do not assume that chain revocation automatically invalidates every access token in every authentication mode.

## 10. Logout Versus Session Revocation

UltimateAuth exposes authentication logout operations through `Flows` and session management operations through `Sessions`.

Both may affect authentication state, but they serve different application responsibilities.

| Operation | Recommended API |
|---|---|
| Normal logout | `UAuthClient.Flows.LogoutAsync()` |
| Logout a selected device | `UAuthClient.Flows.LogoutMyDeviceAsync(request)` |
| Logout other devices | `UAuthClient.Flows.LogoutMyOtherDevicesAsync()` |
| Logout all devices | `UAuthClient.Flows.LogoutAllMyDevicesAsync()` |
| Inspect session chains | `UAuthClient.Sessions.GetMyChainsAsync()` |
| Revoke a selected chain | `UAuthClient.Sessions.RevokeMyChainAsync(chainId)` |
| Revoke other chains | `UAuthClient.Sessions.RevokeMyOtherChainsAsync()` |
| Revoke all chains | `UAuthClient.Sessions.RevokeAllMyChainsAsync()` |

### Which API Should You Use?

For ordinary authentication actions, prefer `Flows`.

```csharp
await UAuthClient.Flows.LogoutAsync();
```

For a session management interface that lists and controls chains, use `Sessions`.

```csharp
var result = await UAuthClient.Sessions.RevokeMyChainAsync(chainId);
```

The APIs expose related capabilities through different client surfaces.

The `Flows` API emphasizes authentication workflows, while `Sessions` exposes the session management model more directly.

### Logout a Device Through Flows

The device logout method requires `LogoutDeviceRequest`.

```csharp
var result = await UAuthClient.Flows.LogoutMyDeviceAsync(
    new LogoutDeviceRequest
    {
        ChainId = chainId
    });
```

Pass a `SessionChainId`, not an individual `AuthSessionId`.

For complete logout examples, see [Authentication](/docs/client/authentication).

## 11. Build a Device Management Page

A common application requirement is to show all devices associated with the current account and allow the user to revoke them.

The following example uses the session client to build a simple Blazor device list.

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Core.Contracts
@using CodeBeam.UltimateAuth.Core.Domain
@inject IUAuthClient UAuthClient

<h3>My Devices</h3>

@if (_loading)
{
    <p>Loading devices...</p>
}
else if (_error is not null)
{
    <p role="alert">@_error</p>
}
else
{
    @foreach (var chain in _chains)
    {
        <div @key="chain.ChainId">
            <strong>@(chain.Browser ?? "Unknown browser")</strong>
            <p>@(chain.OperatingSystem ?? "Unknown OS")</p>
            <p>State: @chain.State</p>

            @if (chain.IsCurrentDevice)
            {
                <span>Current device</span>
            }

            <button type="button" @onclick="() => RevokeAsync(chain.ChainId)">Revoke</button>
        </div>
    }
}

@code {
    private IReadOnlyList<SessionChainSummary> _chains = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var result = await UAuthClient.Sessions.GetMyChainsAsync(new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 20
                });

            if (result.IsSuccess && result.Value is { } page)
            {
                _chains = page.Items;
            }
            else
            {
                _error = "Unable to load devices.";
            }
        }
        catch (Exception)
        {
            _error = "The session service is unavailable.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task RevokeAsync(SessionChainId chainId)
    {
        var result = await UAuthClient.Sessions.RevokeMyChainAsync(chainId);

        if (result.IsSuccess)
        {
            await LoadAsync();
        }
        else
        {
            _error = "Unable to revoke the selected device.";
        }
    }
}
```

This example demonstrates the core integration.

A production UI should additionally provide confirmation before revocation, disable repeated submissions while an operation is in progress, and handle the possibility that the current device is being revoked.

The example displays the first page. Use the pagination API to expose additional chains.

## 12. Administrative Session Management

Administrative operations allow authorized actors to inspect and manage another user's session state.

These methods require appropriate server-side permissions.

The presence of an administrative SDK method does not grant administrative access.

### List a User's Devices

```csharp
var result = await UAuthClient.Sessions.GetUserChainsAsync(userKey);

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var chain in page.Items)
    {
        Console.WriteLine(chain.ChainId);
    }
}
```

The method accepts an optional `PageRequest`.

```csharp
var result = await UAuthClient.Sessions.GetUserChainsAsync(userKey,
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 20
    });
```

### Inspect a User's Chain

Use `GetUserChainDetailAsync`:

```csharp
var result = await UAuthClient.Sessions.GetUserChainDetailAsync(userKey, chainId);

if (result.IsSuccess && result.Value is { } detail)
{
    foreach (var session in detail.Sessions)
    {
        Console.WriteLine(session.SessionId);
    }
}
```

This provides the same `SessionChainDetail` model used for self-service chain inspection.

### Revoke an Individual Session

Use `RevokeUserSessionAsync` when you need to revoke one authentication instance rather than its entire chain.

```csharp
var result = await UAuthClient.Sessions.RevokeUserSessionAsync(userKey, sessionId);

if (result.IsSuccess)
{
    // The session revocation operation succeeded.
}
```

The method requires an `AuthSessionId`.

This is different from chain revocation.

A session-level operation targets a single authentication instance; a chain-level operation targets the associated session continuity boundary.

### Revoke a User's Device Chain

```csharp
var result = await UAuthClient.Sessions.RevokeUserChainAsync(userKey, chainId);

if (result.IsSuccess && result.Value is { } revoke)
{
    // Inspect revoke.CurrentChain or revoke.RootRevoked if needed.
}
```

The method returns `UAuthResult<RevokeResult>`.

### Revoke All User Chains

```csharp
var result = await UAuthClient.Sessions.RevokeAllUserChainsAsync(userKey);

if (result.IsSuccess)
{
    // All applicable user chains were revoked.
}
```

This is useful for administrative account security operations.

### Revoke the User's Session Root

UltimateAuth also exposes root-level revocation:

```csharp
var result = await UAuthClient.Sessions.RevokeUserRootAsync(userKey);

if (result.IsSuccess)
{
    // Root-level revocation succeeded.
}
```

Root revocation is a stronger account-level security operation than revoking one session or one chain.

It is intended for situations where the user's broader session authority must be invalidated.

Examples include account compromise response or other security-critical administrative actions.

Do not use root revocation as a routine substitute for ordinary logout.

## 13. Choosing the Correct Revocation Scope

UltimateAuth provides multiple revocation levels because different security events require different responses.

| Scope | API | Intended effect |
|---|---|---|
| Current logout | `Flows.LogoutAsync()` | End current authentication |
| Individual session | `Sessions.RevokeUserSessionAsync()` | Revoke one authentication instance |
| One chain | `Sessions.RevokeMyChainAsync()` | Revoke a device-associated chain |
| Other chains | `Sessions.RevokeMyOtherChainsAsync()` | Retain current chain, revoke others |
| All chains | `Sessions.RevokeAllMyChainsAsync()` | Revoke all current user's chains |
| User root | `Sessions.RevokeUserRootAsync()` | Invalidate account-level session authority |

Use the narrowest scope that satisfies the security requirement.

For example, removing one unrecognized device from an account should not automatically require revoking the entire user's session root.

Conversely, a confirmed account-wide compromise may justify broader revocation.

## 14. Handling Session Operation Results

Session client methods return `UAuthResult` or `UAuthResult<T>`.

A typical result-handling pattern is:

```csharp
var result = await UAuthClient.Sessions.GetMyChainsAsync();

if (result.IsSuccess && result.Value is { } page)
{
    // Use the returned data.
}
else if (result.Http.IsUnauthorized)
{
    // Authentication is required.
}
else if (result.Http.IsForbidden)
{
    // The operation is not authorized.
}
else
{
    // Handle another failure.
}
```

Available response information includes:

- `IsSuccess`
- `Status`
- `Problem`
- `ErrorText`
- `CorrelationId`
- `TraceId`

The `Http` helper exposes status checks such as `IsBadRequest`, `IsUnauthorized`, `IsForbidden`, and `IsConflict`.

### Administrative Authorization Failures

An administrative session operation may return `403 Forbidden` when the actor lacks the required permission.

```csharp
var result = await UAuthClient.Sessions.GetUserChainsAsync(userKey);

if (result.Http.IsForbidden)
{
    // The current actor cannot inspect this user's sessions.
}
```

Do not attempt to work around authorization failures by switching to a different endpoint.

The server is responsible for enforcing the operation's access policy.

## 15. Session State Events

The session client publishes state events for certain successful revocation operations.

The current implementation publishes `SessionRevoked` when:

- `RevokeMyChainAsync` succeeds and the result identifies the current chain.
- `RevokeAllMyChainsAsync` succeeds.

Other session management methods do not necessarily publish the same event.

Applications should not assume that every successful administrative revocation produces a local state event.

State events help the client update its authentication-related UI.

They do not replace authoritative server-side session validation.

## 16. Security and Operational Considerations

### Device Metadata Is Descriptive

Browser, operating system, platform, and device type are useful for displaying session information.

They are not reliable proof of physical device ownership.

A malicious or modified client may be able to influence some reported metadata.

### Session Chains Are Not Permanent Device Identities

Revoking a chain invalidates its authentication continuity.

It does not permanently ban the physical device.

A later successful login may establish new authentication state.

### Revocation Is Server-Controlled

Client-side removal of a device card does not revoke the underlying session.

Always use the appropriate session or logout operation and inspect its result.

### Authentication Modes Matter

The Root → Chain → Session hierarchy is relevant to server-managed session modes.

Do not assume identical session management semantics in PureJwt or other modes that do not enforce the same hierarchy during ordinary request validation.

### Protect Administrative Operations

Administrative session revocation can interrupt another user's authentication.

These operations should be protected by appropriate permissions and, where required, stronger authentication or administrative confirmation.

## Next Steps

- [Authentication](/docs/client/authentication) — Login, logout, refresh, and authentication state validation.
- [Session Model & Lifecycle](/docs/sessions-devices/session-lifecycle) — Session roots, chains, lifecycle, and expiration.
- [Device Management](/docs/sessions-devices/device-management) — Device-associated authentication contexts.
- [Session Security Model](/docs/security/session-security) — Revocation and session security boundaries.