---
title: Identifiers
order: 5
group: client
---

# Identifiers

UltimateAuth treats user identifiers as independent, managed entities rather than simple fields stored directly on a user profile.

Through `UAuthClient.Identifiers`, applications can manage usernames, email addresses, phone numbers, and custom identifiers, including primary designation, ownership verification, availability checks, and administrative operations.

This guide covers the public `IUserIdentifierClient` API with practical examples for account settings, registration forms, and administration interfaces.

## 1. Understanding User Identifiers

An identifier represents a value associated with a user that may be used for identification, authentication, or communication, depending on the application's configuration.

Supported identifier types are:

```csharp
public enum UserIdentifierType
{
    Username = 0,
    Email = 10,
    Phone = 20,
    Custom = 100
}
```

A single user can have multiple identifiers.

For example:

```text
User
  |
  +-- Username
  |     johndoe                  [Primary]
  |
  +-- Email
  |     john@example.com         [Primary, Verified]
  |     john@business.com        [Verified]
  |
  +-- Phone
        +1 555 0100              [Primary, Verified]
```

Each identifier has its own value, type, verification state, and primary designation.

**Identifiers are not profiles.** A user profile contains information such as display name and biography, while identifiers represent managed identity-related values.

For general profile management, see [User Management](/docs/client/user-management).

## 2. Accessing the Identifier Client

Inject `IUAuthClient` into a Blazor component:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Identifier operations are available through:

```csharp
UAuthClient.Identifiers
```

This property exposes `IUserIdentifierClient`.

The client supports three main categories of operations:

| Category | Purpose |
|---|---|
| Self-service | Manage identifiers belonging to the current user |
| Administration | Manage identifiers belonging to a specified user |
| Availability | Check whether an identifier value is valid and available |

All operations are subject to server-side authorization and identifier policies.

## 3. Get My Identifiers

Use `GetMyAsync` to retrieve identifiers belonging to the authenticated user.

```csharp
var result = await UAuthClient.Identifiers.GetMyAsync();

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var identifier in page.Items)
    {
        Console.WriteLine($"{identifier.Type}: {identifier.Value}");
    }
}
```

The method returns:

```csharp
UAuthResult<PagedResult<UserIdentifierInfo>>
```

### UserIdentifierInfo

Each identifier is represented by `UserIdentifierInfo`.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifier record ID |
| `Type` | `UserIdentifierType` | Identifier type |
| `Value` | `string` | Identifier value |
| `NormalizedValue` | `string` | Server-normalized value |
| `IsPrimary` | `bool` | Whether it is primary for its type |
| `IsVerified` | `bool` | Whether ownership has been verified |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp |
| `VerifiedAt` | `DateTimeOffset?` | Verification timestamp |
| `Version` | `long` | Entity version |

The `Id` identifies the managed identifier record and is used by update, verification, primary designation, and deletion operations.

### Filter Identifiers in the UI

For example, display verified email addresses from a returned page:

```csharp
var result = await UAuthClient.Identifiers.GetMyAsync();

if (result.IsSuccess && result.Value is { } page)
{
    var verifiedEmails = page.Items
        .Where(x =>
            x.Type == UserIdentifierType.Email &&
            x.IsVerified)
        .ToList();
}
```

This filters the current page only. Retrieve additional pages when the application needs the complete collection.

## 4. Pagination

`GetMyAsync` accepts an optional `PageRequest`.

```csharp
var result = await UAuthClient.Identifiers.GetMyAsync(
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 20
    });
```

The server applies its configured pagination policy.

The returned `PagedResult<UserIdentifierInfo>` includes:

- `Items`
- `TotalCount`
- `PageNumber`
- `PageSize`
- `HasNext`

For example:

```csharp
if (result.IsSuccess && result.Value is { } page)
{
    Console.WriteLine($"Total: {page.TotalCount}");

    if (page.HasNext)
    {
        // Enable the next-page control.
    }
}
```

Do not assume the first page contains every identifier.

## 5. Add an Identifier

Use `AddMyAsync` to associate a new identifier with the current user.

### Add an Email Address

```csharp
var result = await UAuthClient.Identifiers.AddMyAsync(
    new AddUserIdentifierRequest
    {
        Type = UserIdentifierType.Email,
        Value = "john@business.com"
    });

if (result.IsSuccess)
{
    // Identifier added.
}
```

### Add a Phone Number

```csharp
var result = await UAuthClient.Identifiers.AddMyAsync(
    new AddUserIdentifierRequest
    {
        Type = UserIdentifierType.Phone,
        Value = "+15550100"
    });
```

### Add a Username

```csharp
var result = await UAuthClient.Identifiers.AddMyAsync(
    new AddUserIdentifierRequest
    {
        Type = UserIdentifierType.Username,
        Value = "johndoe"
    });
```

### Add a Custom Identifier

```csharp
var result = await UAuthClient.Identifiers.AddMyAsync(
    new AddUserIdentifierRequest
    {
        Type = UserIdentifierType.Custom,
        Value = "external-customer-123"
    });
```

Custom identifier support does not automatically mean that arbitrary custom values can be used for authentication.

Login eligibility depends on server configuration and the relevant identifier policies.

### Add as Primary

Set `IsPrimary` when creating the identifier:

```csharp
var result = await UAuthClient.Identifiers.AddMyAsync(
    new AddUserIdentifierRequest
    {
        Type = UserIdentifierType.Email,
        Value = "john@example.com",
        IsPrimary = true
    });
```

The server determines whether the new value can be assigned as primary.

Adding an identifier does not automatically establish verified ownership.

## 6. Identifier Uniqueness and Normalization

UltimateAuth normalizes identifier values and enforces uniqueness within the relevant tenant boundary.

This allows applications to work with consistent identifier representations.

For example, email or username normalization may affect how equivalent input values are compared.

The exact normalization rules depend on the identifier type and server implementation.

### Use Server Results as Authoritative

Do not implement a separate client-side normalization algorithm and assume it exactly matches the server.

Client-side formatting can improve the user experience, but the server must remain responsible for:

- Validating identifier format.
- Normalizing identifier values.
- Enforcing uniqueness.
- Checking tenant ownership.
- Applying identifier-specific policies.

An identifier value that appears available in the UI may become unavailable before the user submits it.

Always handle the result of the actual add or update operation.

## 7. Check Identifier Availability

Use `CheckAvailabilityAsync` to check whether an identifier value is valid and available.

This is useful for registration forms and account settings.

### Check a Username

```csharp
var result = await UAuthClient.Identifiers.CheckAvailabilityAsync(
    new CheckUserIdentifierAvailabilityRequest
    {
        Type = UserIdentifierType.Username,
        Value = "johndoe"
    });

if (result.IsSuccess && result.Value is { } availability)
{
    if (!availability.IsValid)
    {
        // The identifier value is invalid.
    }
    else if (!availability.IsAvailable)
    {
        // The identifier is already unavailable.
    }
    else
    {
        // The identifier is currently available.
    }
}
```

### Check an Email Address

```csharp
var result = await UAuthClient.Identifiers.CheckAvailabilityAsync(
    new CheckUserIdentifierAvailabilityRequest
    {
        Type = UserIdentifierType.Email,
        Value = "john@example.com"
    });
```

### Availability Result

The operation returns:

```csharp
UAuthResult<UserIdentifierAvailabilityResult>
```

The response value contains:

| Property | Description |
|---|---|
| `IsValid` | Whether the value passes validation |
| `IsAvailable` | Whether the value is currently available |
| `NormalizedValue` | Normalized identifier value, when available |
| `Errors` | Validation errors |

### Display Validation Feedback

```csharp
var result = await UAuthClient.Identifiers.CheckAvailabilityAsync(
    new CheckUserIdentifierAvailabilityRequest
    {
        Type = UserIdentifierType.Username,
        Value = username
    });

if (result.IsSuccess && result.Value is { } availability)
{
    if (!availability.IsValid)
    {
        foreach (var error in availability.Errors)
        {
            // Display the appropriate validation feedback.
        }
    }
    else if (!availability.IsAvailable)
    {
        message = "This username is unavailable.";
    }
    else
    {
        message = "This username is available.";
    }
}
```

**An availability check is advisory, not a reservation.**

The value may become unavailable between the availability check and the subsequent creation operation.

For public registration forms, also consider whether exposing detailed availability information could enable account enumeration. Apply appropriate server-side controls.

## 8. Update an Identifier

Use `UpdateMyAsync` to change the value of an existing identifier.

```csharp
var result = await UAuthClient.Identifiers.UpdateMyAsync(
    new UpdateUserIdentifierRequest
    {
        Id = identifierId,
        NewValue = "newemail@example.com"
    });

if (result.IsSuccess)
{
    // Identifier updated.
}
```

`UpdateUserIdentifierRequest` contains:

| Property | Description |
|---|---|
| `Id` | Existing identifier record ID |
| `NewValue` | Replacement identifier value |

Unlike profile updates, this operation targets one identifier record.

It does not replace the user's entire identifier collection.

### Verification After an Update

Changing an identifier's value may require ownership verification again.

For example, verification of an old email address does not prove ownership of a new email address.

Applications should reload the identifier after updating it and inspect its current `IsVerified` state.

Do not assume the previous verification state remains valid.

## 9. Understanding Primary Identifiers

UltimateAuth supports **one primary identifier per type**, rather than one global primary identifier for the entire user.

For example, a user may have:

- One primary username.
- One primary email.
- One primary phone number.

These primary designations are independent.

The primary flag identifies the preferred identifier within its type.

How the application uses primary identifiers for display, communication, or authentication depends on its configuration and business rules.

### Set a Primary Identifier

Use `SetMyPrimaryAsync`:

```csharp
var result = await UAuthClient.Identifiers.SetMyPrimaryAsync(
    new SetPrimaryUserIdentifierRequest
    {
        Id = identifierId
    });

if (result.IsSuccess)
{
    // Primary designation updated.
}
```

When a different identifier becomes primary within the same type, the server maintains the per-type primary constraint.

Setting an email address as primary does not unset the primary phone number or username.

### Unset a Primary Identifier

Use `UnsetMyPrimaryAsync`:

```csharp
var result = await UAuthClient.Identifiers.UnsetMyPrimaryAsync(
    new UnsetPrimaryUserIdentifierRequest
    {
        Id = identifierId
    });
```

Whether a primary identifier can be unset without selecting a replacement depends on server policy.

Applications should handle rejection when the server requires a primary identifier of that type.

### Display Primary Identifiers

```csharp
var result = await UAuthClient.Identifiers.GetMyAsync();

if (result.IsSuccess && result.Value is { } page)
{
    var primaryIdentifiers = page.Items.Where(x => x.IsPrimary).ToList();

    foreach (var identifier in primaryIdentifiers)
    {
        Console.WriteLine($"{identifier.Type}: {identifier.Value}");
    }
}
```

Remember that this example searches only the returned page.

## 10. Verify Identifier Ownership

Verification is a security-sensitive operation.

A user must not be able to mark an email address or phone number as verified merely by knowing or submitting its identifier ID.

UltimateAuth requires ownership proof for self-service verification.

### Verify an Identifier

Use `VerifyMyAsync` with a proof obtained through your application's verification workflow.

```csharp
var result = await UAuthClient.Identifiers.VerifyMyAsync(
    new VerifyUserIdentifierRequest
    {
        Id = identifierId,
        Proof = verificationProof
    });

if (result.IsSuccess)
{
    // Identifier verification succeeded.
}
```

The `Proof` property is required for self-service verification.

For example, a verification workflow may deliver a code to the email address or phone number being verified.

The application then submits the received proof through `VerifyMyAsync`.

The SDK does not itself guarantee delivery of verification messages.

### Verification Flow

A typical verification workflow looks like this:

```text
User adds identifier
        |
        v
Application initiates verification
        |
        v
Verification proof is delivered
        |
        v
User submits proof
        |
        v
Identifiers.VerifyMyAsync(...)
        |
        v
Server validates ownership proof
        |
        +-- Valid --> Identifier verified
        |
        +-- Invalid --> Verification rejected
```

The mechanism used to issue and deliver the proof is application-specific.

The server-side verification implementation must validate the proof before updating the identifier's verified state.

## 11. Server-Side Verification with IUserIdentifierVerifier

UltimateAuth defines `IUserIdentifierVerifier` for validating identifier ownership proof.

```csharp
public interface IUserIdentifierVerifier
{
    Task<bool> VerifyAsync(UserIdentifierVerificationContext context, CancellationToken ct = default);
}
```

The verification context includes:

```csharp
public sealed record UserIdentifierVerificationContext(
    TenantKey Tenant,
    UserKey UserKey,
    Guid IdentifierId,
    UserIdentifierType Type,
    string Value,
    string Proof);
```

A secure verifier must bind the proof to:

- The tenant.
- The user.
- The identifier record.
- The current identifier value.

It must also enforce appropriate expiration, attempt limits, and single-use consumption.

### Register Your Verifier

In the server application, register your implementation:

```csharp
builder.Services.AddScoped<IUserIdentifierVerifier, ApplicationIdentifierVerifier>();
```

`ApplicationIdentifierVerifier` represents your own implementation of the interface.

UltimateAuth includes a `NotConfiguredUserIdentifierVerifier` fallback that throws an explanatory exception when verification is attempted without a configured implementation.

This prevents an unconfigured verifier from silently approving identifier ownership.

**Do not implement a verifier that returns `true` unconditionally in production.**

## 12. Delete an Identifier

Use `DeleteMyAsync` to remove an identifier.

```csharp
var result = await UAuthClient.Identifiers.DeleteMyAsync(
    new DeleteUserIdentifierRequest
    {
        Id = identifierId,
        Mode = DeleteMode.Soft
    });

if (result.IsSuccess)
{
    // Identifier deleted.
}
```

The request includes a deletion mode:

```csharp
public enum DeleteMode
{
    Soft = 0,
    Hard = 10
}
```

### Hard Delete

```csharp
var result = await UAuthClient.Identifiers.DeleteMyAsync(
    new DeleteUserIdentifierRequest
    {
        Id = identifierId,
        Mode = DeleteMode.Hard
    });
```

Deletion behavior depends on the server's identifier lifecycle and authorization rules.

An application should not assume that every caller can select either deletion mode.

### Deleting Primary Identifiers

Deleting a primary identifier may be restricted.

For example, the server may require another identifier of the same type to become primary first.

Applications should inspect the result and provide appropriate feedback.

### Deleting Login Identifiers

Removing an identifier that is used for authentication may affect future login attempts.

Before exposing deletion, consider whether the user will retain a supported method of authentication.

Identifier deletion should not be treated as equivalent to revoking all existing sessions.

## 13. Build an Identifier Management Page

The following example demonstrates a basic Blazor page that lists identifiers, adds an email address, and allows users to change primary designation.

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Core.Contracts
@using CodeBeam.UltimateAuth.Users.Contracts
@inject IUAuthClient UAuthClient

<h3>My Identifiers</h3>

@if (_loading)
{
    <p>Loading identifiers...</p>
}
else
{
    @foreach (var item in _identifiers)
    {
        <div @key="item.Id">
            <strong>@item.Value</strong>
            <span>@item.Type</span>

            @if (item.IsVerified)
            {
                <span>Verified</span>
            }
            else
            {
                <span>Not verified</span>
            }

            @if (item.IsPrimary)
            {
                <span>Primary</span>
            }
            else
            {
                <button type="button" @onclick="() => SetPrimaryAsync(item.Id)">Make primary</button>
            }
        </div>
    }
}

<h4>Add Email</h4>
<input @bind="_newEmail" type="email" placeholder="Email address" />
<button type="button" @onclick="AddEmailAsync" disabled="@_busy">Add email</button>

@if (!string.IsNullOrWhiteSpace(_message))
{
    <p role="status">@_message</p>
}

@code {
    private IReadOnlyList<UserIdentifierInfo> _identifiers = [];

    private string _newEmail = "";
    private string? _message;

    private bool _loading = true;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;

        try
        {
            var result = await UAuthClient.Identifiers.GetMyAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 50
                });

            if (result.IsSuccess && result.Value is { } page)
            {
                _identifiers = page.Items;
            }
            else
            {
                _message = "Unable to load identifiers.";
            }
        }
        catch (Exception)
        {
            _message = "The identifier service is unavailable.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task AddEmailAsync()
    {
        if (_busy || string.IsNullOrWhiteSpace(_newEmail))
            return;

        _busy = true;
        _message = null;

        try
        {
            var result = await UAuthClient.Identifiers.AddMyAsync(
                new AddUserIdentifierRequest
                {
                    Type = UserIdentifierType.Email,
                    Value = _newEmail
                });

            if (result.IsSuccess)
            {
                _newEmail = "";
                await LoadAsync();
            }
            else
            {
                _message = "Unable to add email.";
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task SetPrimaryAsync(Guid id)
    {
        var result = await UAuthClient.Identifiers.SetMyPrimaryAsync(
            new SetPrimaryUserIdentifierRequest
            {
                Id = id
            });

        if (result.IsSuccess)
        {
            await LoadAsync();
        }
        else
        {
            _message = "Unable to update primary identifier.";
        }
    }
}
```

This example demonstrates the core SDK integration.

It displays the first page of identifiers. A production implementation should add pagination, verification actions, deletion confirmation, appropriate exception handling, and loading states for all mutations.

For sensitive changes, consider requiring recent authentication or additional verification according to application policy.

## 14. Administrative Identifier Management

UltimateAuth provides administrative methods for managing identifiers belonging to another user.

These operations require appropriate server-side authorization.

### Get a User's Identifiers

```csharp
var result = await UAuthClient.Identifiers.GetUserAsync(userKey,
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 20
    });

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var identifier in page.Items)
    {
        Console.WriteLine(identifier.Value);
    }
}
```

### Add an Identifier

```csharp
var result = await UAuthClient.Identifiers.AddUserAsync(userKey,
    new AddUserIdentifierRequest
    {
        Type = UserIdentifierType.Email,
        Value = "employee@example.com"
    });
```

### Update an Identifier

```csharp
var result = await UAuthClient.Identifiers.UpdateUserAsync(userKey,
    new UpdateUserIdentifierRequest
    {
        Id = identifierId,
        NewValue = "updated@example.com"
    });
```

### Set Primary

```csharp
var result = await UAuthClient.Identifiers.SetUserPrimaryAsync(userKey,
    new SetPrimaryUserIdentifierRequest
    {
        Id = identifierId
    });
```

### Unset Primary

```csharp
var result = await UAuthClient.Identifiers.UnsetUserPrimaryAsync(userKey,
    new UnsetPrimaryUserIdentifierRequest
    {
        Id = identifierId
    });
```

### Verify an Identifier as Administrator

```csharp
var result = await UAuthClient.Identifiers.VerifyUserAsync(userKey,
    new VerifyUserIdentifierRequest
    {
        Id = identifierId
    });
```

The request contract does not require `Proof` for authorized administrative verification.

However, this does not mean any authenticated caller may bypass ownership verification.

The server must authorize the administrative verification operation and enforce its security requirements.

Administrative verification should be reserved for trusted workflows with appropriate auditability.

### Delete an Identifier

```csharp
var result = await UAuthClient.Identifiers.DeleteUserAsync(userKey,
    new DeleteUserIdentifierRequest
    {
        Id = identifierId,
        Mode = DeleteMode.Soft
    });
```

Administrative operations act on the specified target user.

They do not automatically publish the same local identifier state events as self-service operations.

## 15. Handling Identifier Operation Results

Most identifier mutations return `UAuthResult`.

Retrieval and availability methods return `UAuthResult<T>`.

### Handle Success and Failure

```csharp
var result = await UAuthClient.Identifiers.AddMyAsync(request);

if (result.IsSuccess)
{
    // Operation succeeded.
}
else if (result.Http.IsConflict)
{
    // Identifier conflict.
}
else if (result.Http.IsForbidden)
{
    // Operation not permitted.
}
else if (result.Http.IsBadRequest)
{
    // Invalid request.
}
else
{
    // Handle another failure.
}
```

The actual status code depends on the server's response.

`UAuthResult` also exposes `Problem`, `ErrorText`, `CorrelationId`, and `TraceId`.

Use structured errors when appropriate, but avoid displaying sensitive internal information directly to users.

## 16. Identifier State Events

The client publishes `IdentifiersChanged` after successful self-service identifier mutations.

| Operation | Event |
|---|---|
| `AddMyAsync` | `IdentifiersChanged` |
| `UpdateMyAsync` | `IdentifiersChanged` |
| `SetMyPrimaryAsync` | `IdentifiersChanged` |
| `UnsetMyPrimaryAsync` | `IdentifiersChanged` |
| `VerifyMyAsync` | `IdentifiersChanged` |
| `DeleteMyAsync` | `IdentifiersChanged` |

For add and update operations, the event carries the corresponding request payload.

For primary designation, verification, and deletion, the SDK publishes the event without a request payload.

The event handling mode follows the configured client state event options.

Read operations and availability checks do not publish `IdentifiersChanged`.

Administrative mutations also do not automatically publish this local self-service event.

### Refresh Identifier Data After Changes

A component may respond to an identifier state event by reloading its identifier list.

Alternatively, after a successful mutation, the calling component can explicitly refresh the relevant data.

Client events are synchronization mechanisms, not authoritative security decisions.

## 17. Security Considerations

### Verification Requires Proof

Self-service verification must establish ownership through a valid proof.

Knowing an identifier ID is not sufficient.

The server must validate proof binding, expiration, attempt limits, and single-use consumption.

### Primary Does Not Mean Verified

An identifier's primary designation and verification state are separate properties.

Applications should not assume that a primary email or phone number has been verified.

Check `IsVerified` where verified ownership is required.

### Login Eligibility Is Separate

An identifier may exist on an account without being permitted for login.

Login eligibility depends on the configured authentication and identifier policies.

Do not assume that every custom identifier is automatically an authentication identifier.

### Tenant Boundaries Matter

Identifier uniqueness and ownership are enforced within the relevant tenant context.

Client code must not attempt to bypass tenant isolation by modifying request paths or target user identifiers.

### Availability Checks Are Not Reservations

Availability can change between requests.

Always enforce uniqueness during the actual identifier mutation.

### Administrative Verification Is Privileged

The administrative verification API has different proof requirements from self-service verification.

Only appropriately authorized actors should be able to use it.

### Identifier Changes May Affect Authentication

Changing or deleting a login identifier may affect future authentication.

Applications should distinguish identifier management from session revocation and credential management.

## Next Steps

- [User Management](/docs/client/user-management) — Manage user profiles, account status, and lifecycle.
- [Credentials](/docs/client/credentials) — Manage passwords, credentials, and recovery workflows.
- [Authentication](/docs/client/authentication) — Understand login and authentication flows.
- [Users](/docs/plugin-domains/users) — Explore the user domain architecture.
- [Session Security Model](/docs/security/session-security) — Understand authentication security boundaries.