---
title: Lockout & Recovery
order: 8
group: security
---

# Authentication Lockout & Recovery

Authentication security does not end when a login attempt fails.

Applications need to understand why authentication is blocked, inspect the affected security state, and provide a controlled way to restore access without changing unrelated account or session state.

UltimateAuth treats lockout as **a manageable authentication security state**, rather than just an error returned by the login endpoint.

With the built-in Client SDK, administrators can inspect and unlock a user's authentication state through the same API used for other user management operations.

## Account and Factor Lockouts

UltimateAuth separates authentication security into two scopes:

| Scope | Description |
|---|---|
| **Account** | Authentication security state associated with the user as a whole. |
| **Factor** | Security state associated with a specific credential type, such as Password. |

This distinction allows applications to inspect and manage a password lockout independently of account-level security restrictions.

It also separates authentication lockout from the user's lifecycle status.

For example, a user can have an `Active` account status while their password authentication factor is temporarily locked.

## Inspect Authentication Security State

Use `IUAuthClient.Users.GetAuthenticationLockoutAsync()` to retrieve the current state of a user.

```csharp
var result = await UAuthClient.Users.GetAuthenticationLockoutAsync(userKey,
    new LockoutQueryRequest(AuthenticationSecurityScope.Factor, CredentialType.Password));

if (result.IsSuccess && result.Value is { } state)
{
    Console.WriteLine($"Locked: {state.IsLocked}");
    Console.WriteLine($"Failed attempts: {state.FailedAttempts}");
    Console.WriteLine($"Locked until: {state.LockedUntil}");
}
```

The response includes:

| Property | Description |
|---|---|
| `Scope` | Account or Factor |
| `CredentialType` | Credential type for factor-level state |
| `IsLocked` | Whether the lockout is currently active |
| `FailedAttempts` | Recorded authentication failures |
| `LastFailedAt` | Timestamp of the most recent failure |
| `LockedUntil` | Lockout expiration timestamp |
| `RequiresReauthentication` | Whether an independent reauthentication requirement remains |
| `SecurityVersion` | Version of the authentication security state |

The response does not expose password hashes or credential reset token hashes.

## Unlock Authentication

Administrators can clear a lockout without resetting the user's password or changing their account status.

```csharp
var result = await UAuthClient.Users.UnlockAuthenticationAsync(userKey,
    new UnlockAuthenticationRequest(AuthenticationSecurityScope.Factor, CredentialType.Password));

if (result.IsSuccess && result.Value is { } unlocked)
{
    Console.WriteLine(unlocked.State.IsLocked);
    Console.WriteLine(unlocked.State.FailedAttempts);
}
```

The operation clears the selected security state's failed-attempt count, last-failure timestamp, and lockout expiration.

**Unlocking one scope does not automatically unlock another scope.**

An account-level lockout must be managed separately from a password-factor lockout.

Unlocking also does not change the user's lifecycle status, reset their password, remove reauthentication requirements, or automatically revoke existing sessions.

## Recovery Through Credential Reset

Authentication recovery is also supported through the credential reset workflow.

After a successful, verified credential reset, UltimateAuth consumes the reset proof and clears the relevant password authentication lockout as part of the recovery operation.

This allows a user who has completed the required recovery process to authenticate again without an additional administrator unlock step.

A credential reset and an administrative unlock serve different purposes:

| Operation | Purpose |
|---|---|
| Administrative unlock | Clear a selected authentication lockout |
| Credential recovery | Verify recovery proof, reset the credential, and clear the relevant lockout |

Credential recovery remains subject to the configured credential and session security policies.

## Authorization and Security

Authentication lockout inspection and unlocking are administrative operations.

UltimateAuth defines dedicated actions:

```csharp
UAuthActions.Authentication.GetLockoutAdmin
UAuthActions.Authentication.UnlockAdmin
```

Both operations are executed through the server's access context and authorization pipeline.

Authentication alone is not sufficient: the caller must also satisfy the applicable permission and policy requirements.

The unlock endpoint is treated as an authenticated state-changing operation and is protected by the server's CSRF and rate-limiting infrastructure.

The lockout inspection endpoint uses the standard rate-limit category, while unlock uses the sensitive category.

Applications should restrict these operations to trusted administrative users and avoid exposing lockout information through anonymous endpoints.

## Blazor Integration

The same API works through `IUAuthClient` in Blazor applications.

For example, a user administration dialog can display:

- Account authentication state
- Password-factor authentication state
- Failed attempt count and last failure
- Lockout expiration
- An authorized unlock action

UltimateAuth's sample applications demonstrate this pattern using the shared `UserDetailDialog` component.

No custom authentication endpoints, HTTP request wrappers, or direct persistence access are required.

## Lockout Configuration

Configure lockout behavior through `UAuthServerOptions.Login`:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Login.LockoutDuration = TimeSpan.FromMinutes(15);
    options.Login.FailureWindow = TimeSpan.FromMinutes(15);
    options.Login.ExtendLockOnFailure = false;
});
```

These settings control failed-attempt thresholds, counting windows, lockout duration, and lock extension behavior.

Administrative unlock does not disable these rules. Subsequent authentication attempts remain subject to the configured policy.

## Related Documentation

- [Login Flow](/docs/auth-flows/login-flow)
- [User Management](/docs/client-sdk/user-management)
- [Server Options](/docs/configuration/server-options)
- [Authorization Pipeline](/docs/security/authorization-pipeline)
- [Session Security Model](/docs/security/session-security)
