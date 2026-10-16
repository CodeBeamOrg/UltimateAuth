---
title: Credentials
order: 6
group: client
---

# Credentials

UltimateAuth provides a dedicated client API for managing authentication credentials, including password creation, password changes, credential revocation, account recovery, and administrative credential operations.

Through `UAuthClient.Credentials`, applications can implement credential management without manually constructing requests to the underlying endpoints.

Credential management is security-sensitive. UltimateAuth separates authenticated credential changes from recovery workflows and provides configurable session invalidation policies.

This guide covers the current `ICredentialClient` API, its request and response models, and practical implementation patterns.

## 1. Understanding Credentials

A credential is authentication material associated with a user.

Passwords are the primary credential type supported by the current credential management request contracts.

UltimateAuth also defines a broader `CredentialType` enum for additional authentication mechanisms.

```csharp
public enum CredentialType
{
    Password = 0,
    OneTimeCode = 10,
    EmailOtp = 11,
    SmsOtp = 12,
    Totp = 30,
    Passkey = 100,
    Certificate = 200,
    ApiKey = 210,
    External = 1000
}
```

These enum values do not mean every credential type is currently supported by the credential management API.

For example, `AddCredentialRequest` currently accepts a secret but does not expose a credential type selector.

Do not assume that passkeys, API keys, or external authentication credentials can be created through `AddMyAsync`.

## 2. Accessing the Credential Client

Inject `IUAuthClient` into your Blazor component:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Credential operations are available through:

```csharp
UAuthClient.Credentials
```

The `Credentials` property exposes `ICredentialClient`.

Its operations fall into three groups:

| Group | Purpose |
|---|---|
| Self-service | Add, change, and revoke the current user's credentials |
| Recovery | Begin and complete a credential reset |
| Administration | Manage another user's credentials |

Recovery operations are designed to support users who cannot authenticate normally.

Administrative operations require appropriate server-side authorization.

## 3. Change Your Password

Use `ChangeMyAsync` when an authenticated user knows their current password and wants to replace it.

```csharp
var result = await UAuthClient.Credentials.ChangeMyAsync(
    new ChangeCredentialRequest
    {
        CurrentSecret = currentPassword,
        NewSecret = newPassword
    });

if (result.IsSuccess && result.Value is { IsSuccess: true })
{
    // Password changed successfully.
}
```

The method returns:

```csharp
UAuthResult<CredentialChangeResult>
```

### ChangeCredentialRequest

| Property | Type | Description |
|---|---|---|
| `CurrentSecret` | `string?` | Current password; required for self-service changes |
| `NewSecret` | `string` | New password |

`CurrentSecret` is nullable in the shared request contract because administrative changes do not require the user's existing password.

For self-service password changes, provide the current secret.

### CredentialChangeResult

The response contains:

| Property | Description |
|---|---|
| `IsSuccess` | Whether the credential change succeeded |
| `Error` | Optional operation-level failure |
| `Type` | Credential type affected by the change |

For example:

```csharp
var result = await UAuthClient.Credentials.ChangeMyAsync(
    new ChangeCredentialRequest
    {
        CurrentSecret = currentPassword,
        NewSecret = newPassword
    });

if (!result.IsSuccess)
{
    errorMessage = "Unable to change your password.";
}
else if (result.Value is { IsSuccess: false } change)
{
    errorMessage = change.Error ?? "Password change failed.";
}
else
{
    successMessage = "Your password has been changed.";
}
```

Check both the API-level result and the operation-level result when processing credential changes.

### Client State Event

After a successful `ChangeMyAsync` operation, the SDK publishes:

```csharp
UAuthStateEvent.CredentialsChangedSelf
```

The event uses the configured client state event handling mode.

It can help the application refresh authentication-related UI.

It does not replace the server's credential or session security checks.

## 4. Build a Password Change Form

The following example demonstrates an authenticated password change form in Blazor.

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Credentials.Contracts
@inject IUAuthClient UAuthClient

<h3>Change Password</h3>

<EditForm Model="_model" OnValidSubmit="ChangePasswordAsync">
    <DataAnnotationsValidator />

    <label>Current password</label>
    <InputText type="password" @bind-Value="_model.CurrentPassword" />

    <label>New password</label>
    <InputText type="password" @bind-Value="_model.NewPassword" />

    <button type="submit" disabled="@_busy">Change password</button>
</EditForm>

@if (!string.IsNullOrWhiteSpace(_message))
{
    <p role="status">@_message</p>
}

@code {
    private readonly PasswordChangeModel _model = new();
    private bool _busy;
    private string? _message;

    private async Task ChangePasswordAsync()
    {
        if (_busy)
            return;

        _busy = true;
        _message = null;

        try
        {
            var result = await UAuthClient.Credentials.ChangeMyAsync(
                new ChangeCredentialRequest
                {
                    CurrentSecret = _model.CurrentPassword,
                    NewSecret = _model.NewPassword
                });

            if (result.IsSuccess &&
                result.Value is { IsSuccess: true })
            {
                _model.CurrentPassword = "";
                _model.NewPassword = "";
                _message = "Password changed successfully.";
            }
            else
            {
                _message = "Unable to change password.";
            }
        }
        catch (Exception)
        {
            _message = "The credential service is unavailable.";
        }
        finally
        {
            _busy = false;
        }
    }

    private sealed class PasswordChangeModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string CurrentPassword { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Required]
        public string NewPassword { get; set; } = "";
    }
}
```

This example demonstrates the client integration.

A production form should additionally provide password confirmation, appropriate password policy feedback, and safeguards against repeated submissions.

Never log password values or include them in application telemetry.

## 5. Add a Credential

Use `AddMyAsync` to add a credential to the current user.

```csharp
var result = await UAuthClient.Credentials.AddMyAsync(
    new AddCredentialRequest
    {
        Secret = password
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } added)
{
    Console.WriteLine($"Credential ID: {added.Id}");
}
```

The method returns:

```csharp
UAuthResult<CredentialAddResult>
```

### AddCredentialRequest

The current request contract is:

```csharp
public sealed record AddCredentialRequest
{
    public required string Secret { get; init; }
}
```

Credential type and source selection are not currently exposed by this request.

### CredentialAddResult

The response contains:

| Property | Description |
|---|---|
| `IsSuccess` | Whether the credential was added |
| `Error` | Optional failure information |
| `Id` | Created credential ID, when available |
| `Type` | Created credential type, when available |

Adding a credential is subject to the server's credential policies.

For example, the server may reject attempts to create a duplicate or otherwise unsupported credential.

## 6. Password Change Versus Password Recovery

UltimateAuth distinguishes between two different workflows.

| Workflow | Typical situation | API |
|---|---|---|
| Password change | Authenticated user knows the current password | `ChangeMyAsync` |
| Password recovery | User cannot authenticate using the existing password | `BeginResetAsync` → `CompleteResetAsync` |

A password change verifies the existing secret.

Password recovery uses a separate proof mechanism, such as a reset token or code delivered through a trusted channel.

**Do not use administrative password changes as a replacement for public password recovery.**

## 7. Begin Password Recovery

Use `BeginResetAsync` to initiate a credential recovery workflow.

```csharp
var result = await UAuthClient.Credentials.BeginResetAsync(
    new BeginResetCredentialRequest
    {
        Identifier = "john@example.com",
        CredentialType = CredentialType.Password,
        ResetCodeType = ResetCodeType.Token
    });

if (result.IsSuccess && result.Value is { } reset)
{
    Console.WriteLine($"Expires at: {reset.ExpiresAt}");
}
```

The method returns:

```csharp
UAuthResult<CredentialBeginResetResult>
```

### BeginResetCredentialRequest

| Property | Description |
|---|---|
| `Identifier` | User identifier associated with the recovery request |
| `CredentialType` | Credential type to recover |
| `ResetCodeType` | Reset proof format |

### Reset Code Types

```csharp
public enum ResetCodeType
{
    Token = 0,
    Code = 10
}
```

Use `Token` for a token-based recovery workflow.

Use `Code` for a code-based recovery workflow.

The delivery mechanism is determined by the application's configured server-side notification implementation.

### CredentialBeginResetResult

The result contains:

```csharp
public sealed record CredentialBeginResetResult
{
    public DateTimeOffset ExpiresAt { get; init; }
}
```

**The reset token or code is not returned in the API response.**

This prevents the recovery endpoint from directly exposing the secret needed to take over an account.

The proof must be delivered through the configured trusted notification channel.

## 8. Complete Password Recovery

After the user receives a reset token or code, call `CompleteResetAsync`.

```csharp
var result = await UAuthClient.Credentials.CompleteResetAsync(
    new CompleteResetCredentialRequest
    {
        Identifier = "john@example.com",
        CredentialType = CredentialType.Password,
        ResetToken = resetToken,
        NewSecret = newPassword
    });

if (result.IsSuccess && result.Value is { IsSuccess: true })
{
    // Password recovery completed.
}
```

The method returns:

```csharp
UAuthResult<CredentialActionResult>
```

### CompleteResetCredentialRequest

| Property | Description |
|---|---|
| `Identifier` | Identifier used for the recovery workflow |
| `CredentialType` | Credential type being recovered |
| `ResetToken` | Delivered recovery proof |
| `NewSecret` | Replacement secret |

The property is named `ResetToken` even when the selected reset format is a code.

Use the exact contract property name.

### CredentialActionResult

```csharp
public sealed record CredentialActionResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
}
```

For example:

```csharp
if (!result.IsSuccess)
{
    errorMessage = "Unable to complete password recovery.";
}
else if (result.Value is { IsSuccess: false } action)
{
    errorMessage = action.Error ?? "Password recovery failed.";
}
else
{
    successMessage = "Your password has been reset.";
}
```

After a successful `CompleteResetAsync` operation, the SDK publishes:

```csharp
UAuthStateEvent.CredentialsChanged
```

The event is not the same as `CredentialsChangedSelf`, which is published by `ChangeMyAsync`.

## 9. Complete Recovery Using a Numeric Code

UltimateAuth supports code-based recovery through `ResetCodeType.Code`.

Begin the recovery:

```csharp
var begin = await UAuthClient.Credentials.BeginResetAsync(
    new BeginResetCredentialRequest
    {
        Identifier = email,
        CredentialType = CredentialType.Password,
        ResetCodeType = ResetCodeType.Code
    });
```

Then submit the code received by the user:

```csharp
var complete = await UAuthClient.Credentials.CompleteResetAsync(
    new CompleteResetCredentialRequest
    {
        Identifier = email,
        CredentialType = CredentialType.Password,
        ResetToken = enteredCode,
        NewSecret = newPassword
    });
```

The same completion API handles token-based and code-based recovery.

The server is responsible for validating the submitted proof against the active recovery state.

## 10. Build a Forgot Password Form

A typical forgot-password interface has two stages:

1. Request a recovery token or code.
2. Submit the received proof with a new password.

### Step One: Request Recovery

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Core.Domain
@using CodeBeam.UltimateAuth.Credentials.Contracts
@inject IUAuthClient UAuthClient

<h3>Forgot Password</h3>

<input @bind="_email" type="email" placeholder="Email address" />

<button type="button" disabled="@_busy" @onclick="BeginRecoveryAsync">Send recovery instructions</button>

@if (_message is not null)
{
    <p role="status">@_message</p>
}

@code {
    private string _email = "";
    private string? _message;
    private bool _busy;

    private async Task BeginRecoveryAsync()
    {
        if (_busy || string.IsNullOrWhiteSpace(_email))
            return;

        _busy = true;

        try
        {
            var result = await UAuthClient.Credentials.BeginResetAsync(
                new BeginResetCredentialRequest
                {
                    Identifier = _email,
                    CredentialType = CredentialType.Password,
                    ResetCodeType = ResetCodeType.Token
                });

            _message = result.IsSuccess
                ? "If the account is eligible, recovery instructions will be sent."
                : "Unable to process the recovery request.";
        }
        catch (Exception)
        {
            _message = "Recovery is temporarily unavailable.";
        }
        finally
        {
            _busy = false;
        }
    }
}
```

The confirmation message intentionally avoids revealing whether an account exists.

Applications should avoid introducing account enumeration through UI messages, timing differences, or diagnostic details.

### Step Two: Complete Recovery

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Core.Domain
@using CodeBeam.UltimateAuth.Credentials.Contracts
@inject IUAuthClient UAuthClient

<h3>Reset Password</h3>

<input @bind="_email" placeholder="Email address" />
<input @bind="_token" placeholder="Recovery token or code" />
<input @bind="_newPassword" type="password" placeholder="New password" />

<button type="button" disabled="@_busy" @onclick="CompleteRecoveryAsync">Reset password</button>

@if (_message is not null)
{
    <p role="status">@_message</p>
}

@code {
    private string _email = "";
    private string _token = "";
    private string _newPassword = "";

    private string? _message;
    private bool _busy;

    private async Task CompleteRecoveryAsync()
    {
        if (_busy)
            return;

        _busy = true;
        _message = null;

        try
        {
            var result = await UAuthClient.Credentials.CompleteResetAsync(
                new CompleteResetCredentialRequest
                {
                    Identifier = _email,
                    CredentialType = CredentialType.Password,
                    ResetToken = _token,
                    NewSecret = _newPassword
                });

            if (result.IsSuccess && result.Value is { IsSuccess: true })
            {
                _token = "";
                _newPassword = "";
                _message = "Password reset successfully.";
            }
            else
            {
                _message = "Unable to reset password.";
            }
        }
        catch (Exception)
        {
            _message = "Recovery is temporarily unavailable.";
        }
        finally
        {
            _busy = false;
        }
    }
}
```

These examples show the SDK integration.

A production implementation should also validate user input, protect recovery proofs from accidental disclosure, and avoid logging reset tokens or passwords.

## 11. Configure Reset Notifications

UltimateAuth uses `ICredentialResetNotifier` to deliver recovery tokens and codes.

```csharp
public interface ICredentialResetNotifier
{
    Task NotifyAsync(CredentialResetNotification notification, CancellationToken ct = default);
}
```

The notification includes:

```csharp
public sealed record CredentialResetNotification(
    TenantKey Tenant,
    UserKey UserKey,
    CredentialType CredentialType,
    ResetCodeType CodeType,
    string Token,
    DateTimeOffset ExpiresAt);
```

The application must provide a secure delivery implementation.

### Register the Notifier

```csharp
builder.Services.AddScoped<ICredentialResetNotifier, ApplicationCredentialResetNotifier>();
```

`ApplicationCredentialResetNotifier` is an application-defined implementation.

It might deliver the proof through email, SMS, or another trusted channel associated with the user's account.

The notifier should not expose the proof to an arbitrary address supplied by an unauthenticated caller.

### Missing Notifier Configuration

UltimateAuth provides a `NotConfiguredCredentialResetNotifier` fallback.

When recovery requires notification but no implementation has been configured, the fallback throws an explanatory exception.

This prevents a reset flow from silently succeeding without a configured proof delivery mechanism.

**Never return recovery tokens to anonymous clients as a substitute for configuring the notifier.**

## 12. Recovery Security Behavior

Credential recovery is designed around several security properties.

### Account Enumeration Resistance

Recovery initiation should not reveal whether a submitted identifier belongs to an account.

Applications should preserve this behavior in their own UI and logging policies.

### Proof Expiration

Recovery proofs have a server-controlled lifetime.

The client does not choose an arbitrary expiration time.

### Single-Use Proofs

A successfully consumed recovery proof must not be reusable.

After successful completion, a second attempt using the same proof should be rejected.

### Identifier and Target Binding

Recovery must apply to the correct user and credential.

Administrative recovery must not permit an identifier belonging to one user to reset another user's credential.

### Session Revocation

Successful recovery may invalidate existing sessions according to the configured credential session policy.

By default, UltimateAuth revokes all session chains after recovery.

For further details, see [Session Security Model](/docs/security/session-security).

## 13. Revoke Your Credential

Use `RevokeMyAsync` to revoke a credential belonging to the authenticated user.

```csharp
var result = await UAuthClient.Credentials.RevokeMyAsync(
    new RevokeCredentialRequest
    {
        Id = credentialId
    });

if (result.IsSuccess)
{
    // Credential revoked.
}
```

The request contract is:

```csharp
public sealed record RevokeCredentialRequest
{
    public Guid Id { get; init; }
}
```

Use the credential record ID, not a user ID or session ID.

Credential revocation is distinct from credential deletion.

Revocation changes the credential's usability, while deletion uses a separate lifecycle operation available through the administrative API.

### State Event

After successful `RevokeMyAsync`, the SDK publishes:

```csharp
UAuthStateEvent.CredentialsChanged
```

The application can use this event to refresh credential-related UI.

Credential revocation may affect authentication eligibility and session continuation depending on server policy.

## 14. Administrative Credential Management

Administrative credential operations target a specific user.

They require appropriate server-side authorization.

### Add a Credential

```csharp
var result = await UAuthClient.Credentials.AddUserAsync(userKey,
    new AddCredentialRequest
    {
        Secret = temporaryPassword
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } added)
{
    Console.WriteLine(added.Id);
}
```

The server validates whether the administrative actor may add a credential for the target user.

### Change a User's Password

```csharp
var result = await UAuthClient.Credentials.ChangeUserAsync(userKey,
    new ChangeCredentialRequest
    {
        NewSecret = newPassword
    });

if (result.IsSuccess && result.Value is { IsSuccess: true })
{
    // Administrative password change succeeded.
}
```

`CurrentSecret` is not required for an authorized administrative credential change.

This makes administrative credential changes a privileged operation.

Do not expose this capability through ordinary self-service UI.

### Revoke a User's Credential

```csharp
var result = await UAuthClient.Credentials.RevokeUserAsync(userKey,
    new RevokeCredentialRequest
    {
        Id = credentialId
    });
```

### Begin Administrative Recovery

```csharp
var result = await UAuthClient.Credentials.BeginResetUserAsync(userKey,
    new BeginResetCredentialRequest
    {
        Identifier = userEmail,
        CredentialType = CredentialType.Password,
        ResetCodeType = ResetCodeType.Token
    });
```

### Complete Administrative Recovery

```csharp
var result = await UAuthClient.Credentials.CompleteResetUserAsync(userKey,
    new CompleteResetCredentialRequest
    {
        Identifier = userEmail,
        CredentialType = CredentialType.Password,
        ResetToken = recoveryToken,
        NewSecret = newPassword
    });
```

Administrative recovery remains a proof-based workflow.

The server validates the relationship between the target user and the supplied identifier.

### Delete a User's Credential

```csharp
var result = await UAuthClient.Credentials.DeleteUserAsync(userKey,
    new DeleteCredentialRequest
    {
        Id = credentialId,
        Mode = DeleteMode.Soft
    });
```

### Hard Delete

```csharp
var result = await UAuthClient.Credentials.DeleteUserAsync(userKey,
    new DeleteCredentialRequest
    {
        Id = credentialId,
        Mode = DeleteMode.Hard
    });
```

Hard deletion is a destructive administrative operation.

Its availability and effects depend on server authorization and credential lifecycle policies.

Administrative operations do not automatically publish the same local credential state events as self-service operations.

## 15. Credential Changes and Session Invalidation

UltimateAuth allows applications to configure how existing sessions are handled after credential changes and recovery.

The server exposes:

```csharp
UAuthServerOptions.CredentialSessionPolicy
```

The policy contains:

| Option | Default | Purpose |
|---|---|---|
| `OnSelfChange` | `PreserveCurrent` | Sessions after self-service credential changes |
| `OnAdminChange` | `RevokeAll` | Sessions after administrative credential changes |
| `OnRecovery` | `RevokeAll` | Sessions after credential recovery |

### CredentialSessionBehavior

```csharp
public enum CredentialSessionBehavior
{
    PreserveAll = 0,
    PreserveCurrent = 10,
    RevokeAll = 20
}
```

| Behavior | Meaning |
|---|---|
| `PreserveAll` | Preserve existing session chains |
| `PreserveCurrent` | Preserve the current chain and revoke other chains |
| `RevokeAll` | Revoke all applicable session chains |

### Configure the Policy

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.CredentialSessionPolicy.OnSelfChange = CredentialSessionBehavior.PreserveCurrent;
    options.CredentialSessionPolicy.OnAdminChange = CredentialSessionBehavior.RevokeAll;
    options.CredentialSessionPolicy.OnRecovery = CredentialSessionBehavior.RevokeAll;
});
```

The defaults are intended to balance usability and security.

For example, an authenticated user changing their password can retain their current device while other devices are invalidated.

An administrator changing a user's password typically requires broader session revocation.

### Recovery and PreserveCurrent

`PreserveCurrent` is not meaningful for many recovery workflows because the user may not have an authenticated current session.

UltimateAuth's recovery policy documentation specifies that `PreserveCurrent` is treated as `RevokeAll` in this context.

### Authentication Mode Considerations

The precise effect of session invalidation depends on the authentication mode.

In particular, a locally validated JWT access token may remain usable until expiration if the resource server does not consult current revocation state.

Credential session policies should be understood together with the application's token validation strategy.

See [Token Security](/docs/security/token-security).

## 16. Handling Credential Results

Credential operations use `UAuthResult` or `UAuthResult<T>`.

Several operations also return a domain-level success flag.

### Credential Creation

```csharp
var result = await UAuthClient.Credentials.AddMyAsync(request);

if (!result.IsSuccess)
{
    // API-level failure.
}
else if (result.Value is { IsSuccess: false } add)
{
    // Credential creation was rejected.
    Console.WriteLine(add.Error);
}
else if (result.Value is { IsSuccess: true } added)
{
    Console.WriteLine(added.Id);
}
```

### Credential Recovery

```csharp
var result = await UAuthClient.Credentials.CompleteResetAsync(request);

if (!result.IsSuccess)
{
    // API-level failure.
}
else if (result.Value is { IsSuccess: false } action)
{
    // Recovery did not complete successfully.
}
else
{
    // Recovery succeeded.
}
```

### HTTP Status Helpers

```csharp
if (result.Http.IsUnauthorized)
{
    // Authentication required.
}
else if (result.Http.IsForbidden)
{
    // Operation not permitted.
}
else if (result.Http.IsConflict)
{
    // Conflicting or expired operation state.
}
else if (result.Http.IsBadRequest)
{
    // Invalid request.
}
```

The actual status depends on the server's response.

Avoid exposing sensitive internal credential errors directly to end users.

## 17. Credential State Events

The current `UAuthCredentialClient` publishes the following events after successful self-service operations.

| Operation | Event |
|---|---|
| `ChangeMyAsync` | `CredentialsChangedSelf` |
| `RevokeMyAsync` | `CredentialsChanged` |
| `CompleteResetAsync` | `CredentialsChanged` |

The following operations do not publish those events in the current client implementation:

- `AddMyAsync`
- `BeginResetAsync`
- Administrative credential operations

Applications should refresh relevant data explicitly where needed.

Events help synchronize client state but do not themselves establish that a credential or session has been invalidated on the server.

## 18. Security Considerations

### Never Expose Stored Secrets

Credential secrets must not be returned through credential management responses.

Applications should never log passwords, reset proofs, or other authentication secrets.

### Use Secure Transport

Credential operations must be performed over appropriately secured transport.

### Protect Recovery Proofs

Recovery tokens and codes should be short-lived, single-use, and delivered through trusted channels.

Do not store recovery proofs in persistent client storage unless the application has a carefully justified security requirement.

### Keep Self-Service and Administrative Operations Separate

Self-service changes require the current credential.

Administrative changes are privileged and require server-side authorization.

Do not substitute one for the other.

### Preserve Account Recovery Safety

Avoid revealing account existence through public recovery responses.

Apply appropriate rate limiting, proof attempt limits, and abuse controls.

### Consider Session Consequences

Changing or resetting a password can affect existing sessions.

Applications should explain the expected consequences to users, especially when other devices will be logged out.

### Do Not Assume Every Credential Type Is Implemented

The existence of a `CredentialType` enum member does not establish that its complete lifecycle is supported.

Use the currently implemented credential contracts and documented authentication flows.

## Next Steps

- [Authentication](/docs/client/authentication) — Login, logout, refresh, and authentication flows.
- [Identifiers](/docs/client/identifiers) — Manage user identifiers and ownership verification.
- [User Management](/docs/client/user-management) — Manage profiles and user lifecycle.
- [Session Management](/docs/client/session-management) — Inspect and revoke sessions.
- [Token Security](/docs/security/token-security) — Understand token validation and revocation behavior.
- [Session Security Model](/docs/security/session-security) — Understand session invalidation and account-level security boundaries.