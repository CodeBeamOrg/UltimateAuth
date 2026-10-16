---
title: User Management
order: 4
group: client
---

# User Management

The UltimateAuth Client SDK provides a comprehensive API for managing users, profiles, account status, and user lifecycle operations.

Through `UAuthClient.Users`, applications can implement account settings, profile management, user registration, and administrative user management without manually constructing API requests.

UltimateAuth distinguishes between **user identity**, **user profiles**, and **account lifecycle state**.

A user has a stable identity and may have multiple profiles. Account status controls lifecycle behavior independently of profile data.

This guide covers the public `IUserClient` API, including self-service and administrative operations, structured results, pagination, multi-profile support, and important update semantics.

For email addresses, usernames, phone numbers, and identifier verification, see [Identifiers](/docs/client/identifiers).

## 1. Accessing the User Client

In a Blazor component, inject `IUAuthClient`:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

User operations are available through:

```csharp
UAuthClient.Users
```

The `Users` property exposes `IUserClient`.

Its operations are divided into three functional areas:

| Area | Capabilities |
|---|---|
| User lifecycle | Create users, change status, delete accounts |
| Self-service profiles | Retrieve, update, create, and delete your own profiles |
| Administration | Query users and manage another user's account and profiles |

Self-service methods operate on the authenticated user.

Administrative methods operate on an explicitly identified target and require the appropriate server-side authorization.

## 2. Get the Current User

Use `GetMeAsync` to retrieve the current user's profile.

```csharp
var result = await UAuthClient.Users.GetMeAsync();

if (result.IsSuccess && result.Value is { } user)
{
    Console.WriteLine(user.DisplayName);
    Console.WriteLine(user.Status);
    Console.WriteLine(user.UserKey);
}
```

The method returns:

```csharp
UAuthResult<UserView>
```

When no profile is specified, UltimateAuth returns the default profile.

### UserView

`UserView` contains account information and the selected profile's data.

| Property | Description |
|---|---|
| `UserKey` | Stable user identifier |
| `Status` | Current account status |
| `UserName` | Username |
| `PrimaryEmail` | Primary email address |
| `PrimaryPhone` | Primary phone number |
| `ProfileKey` | Selected profile |
| `FirstName`, `LastName` | Profile name fields |
| `DisplayName` | Display name |
| `Bio` | Biography |
| `BirthDate` | Birth date |
| `Gender` | Profile gender field |
| `Language` | Preferred language |
| `TimeZone` | Preferred time zone |
| `Culture` | Culture preference |
| `EmailVerified`, `PhoneVerified` | Identifier verification information |
| `CreatedAt` | Account creation timestamp |
| `Metadata` | Custom profile metadata |

The returned profile is a view of server-managed user information.

It should not be treated as a substitute for authoritative authentication or authorization state.

## 3. Update the Current User's Profile

Use `UpdateMyProfileAsync` to update the authenticated user's profile.

```csharp
var result = await UAuthClient.Users.UpdateMyProfileAsync(
    new UpdateProfileRequest
    {
        DisplayName = "John Doe",
        FirstName = "John",
        LastName = "Doe",
        Language = "en",
        TimeZone = "Europe/London"
    });

if (result.IsSuccess)
{
    // Profile update succeeded.
}
```

The method returns `UAuthResult`.

### Important: Profile Updates Replace Editable Fields

`UpdateProfileRequest` uses replacement semantics rather than partial-update semantics.

This means:

- Editable fields supplied in the request replace their previous values.
- Explicit `null` values clear existing values.
- Omitted editable properties are also treated as `null`.

For example:

```csharp
await UAuthClient.Users.UpdateMyProfileAsync(
    new UpdateProfileRequest
    {
        DisplayName = "John Doe"
    });
```

This does **not** mean “change only the display name.”

Other editable profile fields may be cleared.

### Recommended: Read, Modify, and Submit

When building a profile editor, retrieve the current profile and preserve values that should remain unchanged.

```csharp
var currentResult = await UAuthClient.Users.GetMeAsync();

if (!currentResult.IsSuccess || currentResult.Value is not { } current)
{
    return;
}

var update = new UpdateProfileRequest
{
    FirstName = current.FirstName,
    LastName = current.LastName,
    DisplayName = "John Doe",
    BirthDate = current.BirthDate,
    Gender = current.Gender,
    Bio = current.Bio,
    Language = current.Language,
    TimeZone = current.TimeZone,
    Culture = current.Culture,
    Metadata = current.Metadata
};

var result = await UAuthClient.Users.UpdateMyProfileAsync(update);
```

This preserves the values returned by the initial read, but it is not an atomic patch operation.

If another request modifies the profile between the read and update, the later replacement may overwrite those changes.

Applications with concurrent profile editing requirements should account for this behavior.

### Profile State Events

After a successful `UpdateMyProfileAsync` operation, the client publishes a `ProfileChanged` event.

The event carries the `UpdateProfileRequest` used for the operation.

The configured client state event handling mode determines how the application processes the notification.

The event is published only when the SDK operation reports success.

## 4. Understanding Multiple Profiles

UltimateAuth supports multiple profiles for a single user when enabled through server configuration.

For example, one account may have:

```text
User
  |
  +-- default
  |
  +-- business
  |
  +-- public
```

All profiles belong to the same user identity.

They do not represent separate authentication accounts or independently authenticated principals.

Profiles allow applications to organize different sets of user-facing information under one account.

### Default Profile

Every newly created user receives a default profile.

Its key is:

```csharp
ProfileKey.Default
```

The default profile cannot be deleted.

When a profile request omits `ProfileKey`, the default profile is selected.

### Profile Keys

`ProfileKey` is a strongly typed value.

Use `TryCreate` to construct a custom key:

```csharp
if (!ProfileKey.TryCreate("business", out var profileKey))
{
    throw new InvalidOperationException("Invalid profile key.");
}
```

Profile keys are normalized by trimming whitespace and converting the value to lowercase.

A valid key must not be empty or exceed 64 characters.

## 5. Retrieve a Specific Profile

Use `GetProfileRequest` to select a profile.

```csharp
if (!ProfileKey.TryCreate("business", out var profileKey))
{
    return;
}

var result = await UAuthClient.Users.GetMeAsync(
    new GetProfileRequest
    {
        ProfileKey = profileKey
    });

if (result.IsSuccess && result.Value is { } profile)
{
    Console.WriteLine(profile.DisplayName);
    Console.WriteLine(profile.ProfileKey);
}
```

The response is still `UserView`.

The account identity and status remain associated with the same user; the selected profile determines which profile information is returned.

## 6. Create an Additional Profile

Use `CreateMyProfileAsync` to create a new profile for the authenticated user.

```csharp
if (!ProfileKey.TryCreate("business", out var profileKey))
{
    return;
}

var result = await UAuthClient.Users.CreateMyProfileAsync(
    new CreateProfileRequest
    {
        ProfileKey = profileKey,
        DisplayName = "John Doe Consulting",
        Bio = "Business profile",
        Language = "en"
    });

if (result.IsSuccess)
{
    // Profile created.
}
```

`CreateProfileRequest` requires `ProfileKey`.

It also supports the same general profile information used for user-facing profiles.

### Clone an Existing Profile

A new profile can be created using an existing profile as its source.

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.CreateMyProfileAsync(
    new CreateProfileRequest
    {
        ProfileKey = businessKey,
        CloneFrom = ProfileKey.Default
    });
```

`CloneFrom` identifies the source profile.

This is useful when creating a new profile that should initially reuse information from an existing one.

The exact merge and override behavior is determined by the server's profile creation implementation.

### Profile Creation Events

After successful self-service profile creation, the SDK publishes `ProfileChanged`.

This allows application components to refresh profile-related views.

## 7. Update a Specific Profile

Set `ProfileKey` in `UpdateProfileRequest` to select the profile being updated.

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.UpdateMyProfileAsync(
    new UpdateProfileRequest
    {
        ProfileKey = businessKey,
        DisplayName = "John Doe Consulting",
        Bio = "Updated business profile"
    });
```

The same replacement semantics apply to custom profiles.

If the application intends to preserve existing fields, retrieve the selected profile first and include the values that should remain unchanged.

## 8. Delete a Profile

Use `DeleteMyProfileAsync` to remove a custom profile.

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.DeleteMyProfileAsync(
    new DeleteProfileRequest
    {
        ProfileKey = businessKey
    });

if (result.IsSuccess)
{
    // Profile deleted.
}
```

The default profile cannot be deleted.

Deleting a profile is different from deleting the user account.

The user identity and other profiles remain subject to their own lifecycle and server-side state.

On successful self-service profile deletion, the SDK publishes `ProfileChanged` with the deleted profile key.

## 9. Create a User

Use `CreateAsync` to request user creation through the standard user creation endpoint.

```csharp
var result = await UAuthClient.Users.CreateAsync(
    new CreateUserRequest
    {
        UserName = "johndoe",
        Email = "john@example.com",
        Password = password,
        FirstName = "John",
        LastName = "Doe",
        DisplayName = "John Doe"
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } created)
{
    Console.WriteLine($"Created user: {created.UserKey}");
}
```

The method returns:

```csharp
UAuthResult<UserCreateResult>
```

User creation availability depends on the server's configuration and access policies.

### CreateUserRequest

The request supports several groups of fields:

| Category | Properties |
|---|---|
| Identifiers | `UserName`, `Email`, `Phone` |
| Verification flags | `UserNameVerified`, `EmailVerified`, `PhoneVerified` |
| Credentials | `Password` |
| Profile | `FirstName`, `LastName`, `DisplayName`, `BirthDate`, `Gender`, `Bio` |
| Preferences | `Language`, `TimeZone`, `Culture` |
| Extensions | `Metadata` |

Verification flags must not be treated as client-controlled proof of identifier ownership.

The server is responsible for enforcing verification requirements and determining whether a caller is authorized to establish verified identifier state.

For identifier-specific operations and verification workflows, see [Identifiers](/docs/client/identifiers).

### UserCreateResult

The returned value includes:

```csharp
public sealed record UserCreateResult
{
    public required bool IsSuccess { get; init; }
    public string? UserKey { get; init; }
    public string? FailureReason { get; init; }
}
```

The `UserKey` is returned as a string when creation succeeds.

Check both the transport-level `UAuthResult.IsSuccess` and the operation-level `UserCreateResult.IsSuccess` where applicable.

## 10. Administrative User Creation

Use `CreateAsAdminAsync` when creating a user through an administrative context.

```csharp
var result = await UAuthClient.Users.CreateAsAdminAsync(
    new CreateUserRequest
    {
        UserName = "newemployee",
        Email = "employee@example.com",
        DisplayName = "New Employee"
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } created)
{
    Console.WriteLine(created.UserKey);
}
```

Administrative creation is distinct from ordinary user creation.

The server enforces the required permissions and applies the relevant user creation policies.

Applications should not assume that calling the administrative endpoint bypasses validation or identifier ownership rules.

## 11. Query Users

Use `QueryAsync` to search and paginate users.

```csharp
var result = await UAuthClient.Users.QueryAsync(
    new UserQuery
    {
        Search = "john",
        PageNumber = 1,
        PageSize = 20
    });

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var user in page.Items)
    {
        Console.WriteLine(user.DisplayName);
        Console.WriteLine(user.Status);
    }
}
```

The method returns:

```csharp
UAuthResult<PagedResult<UserSummary>>
```

### UserQuery Filters

`UserQuery` extends `PageRequest` and supports:

| Property | Description |
|---|---|
| `Search` | Search term |
| `Status` | Filter by user status |
| `IncludeDeleted` | Include deleted users when authorized |
| `ProfileKey` | Select the profile context for the query |
| `PageNumber` | Page number |
| `PageSize` | Page size |
| `SortBy` | Optional sort field |
| `Descending` | Descending sort order |

### Search Behavior

The query model documents case-insensitive partial matching for:

- Display name.
- Username.
- Primary email.
- Primary phone.

A complete valid `UserKey` can also be used as a search term.

Partial `UserKey` matching is not supported.

### Filter by Status

```csharp
var result = await UAuthClient.Users.QueryAsync(
    new UserQuery
    {
        Status = UserStatus.Active,
        PageNumber = 1,
        PageSize = 20
    });
```

### Include Deleted Users

```csharp
var result = await UAuthClient.Users.QueryAsync(
    new UserQuery
    {
        IncludeDeleted = true,
        PageNumber = 1,
        PageSize = 20
    });
```

The ability to query deleted accounts depends on server-side authorization and data retention behavior.

## 12. UserSummary and Pagination

`QueryAsync` returns `UserSummary` items.

Each summary contains:

| Property | Description |
|---|---|
| `UserKey` | User identifier |
| `UserName` | Username |
| `DisplayName` | Display name |
| `PrimaryEmail` | Primary email |
| `PrimaryPhone` | Primary phone |
| `Status` | Account status |
| `CreatedAt` | Creation timestamp |

This model is suitable for administrative user lists.

### Navigating Pages

```csharp
var result = await UAuthClient.Users.QueryAsync(
    new UserQuery
    {
        PageNumber = 2,
        PageSize = 25
    });

if (result.IsSuccess && result.Value is { } page)
{
    Console.WriteLine($"Total users: {page.TotalCount}");
    Console.WriteLine($"Current page: {page.PageNumber}");
    Console.WriteLine($"Has next page: {page.HasNext}");
}
```

Use `PagedResult<T>` rather than assuming every query returns the complete user collection.

## 13. Get a Specific User

Use `GetUserAsync` to retrieve a user's profile through the administrative API.

```csharp
var result = await UAuthClient.Users.GetUserAsync(userKey);

if (result.IsSuccess && result.Value is { } user)
{
    Console.WriteLine(user.DisplayName);
    Console.WriteLine(user.Status);
}
```

The method returns `UAuthResult<UserView>`.

When no profile is specified, the default profile is selected.

### Retrieve a Specific User Profile

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.GetUserAsync(
    userKey,
    new GetProfileRequest
    {
        ProfileKey = businessKey
    });
```

The actor must have permission to access the target user's requested information.

## 14. Update Another User's Profile

Use `UpdateUserProfileAsync` to update a selected user's profile.

```csharp
var result = await UAuthClient.Users.UpdateUserProfileAsync(
    userKey,
    new UpdateProfileRequest
    {
        DisplayName = "Updated Display Name",
        FirstName = "John",
        LastName = "Doe"
    });

if (result.IsSuccess)
{
    // Profile updated.
}
```

This operation uses the same replacement semantics as `UpdateMyProfileAsync`.

Omitted editable fields may be cleared.

For partial UI editing, retrieve the target profile and preserve values that should not change.

### Update a Named Profile

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.UpdateUserProfileAsync(userKey,
    new UpdateProfileRequest
    {
        ProfileKey = businessKey,
        DisplayName = "Updated Business Profile"
    });
```

Administrative profile updates require the appropriate authorization for the target user.

## 15. Manage Another User's Profiles

Administrators can create and delete profiles for a selected user.

### Create a Profile

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.CreateUserProfileAsync(userKey,
    new CreateProfileRequest
    {
        ProfileKey = businessKey,
        DisplayName = "Business Profile"
    });
```

### Delete a Profile

```csharp
ProfileKey.TryCreate("business", out var businessKey);

var result = await UAuthClient.Users.DeleteUserProfileAsync(userKey,
    new DeleteProfileRequest
    {
        ProfileKey = businessKey
    });
```

The default profile cannot be deleted.

Administrative profile operations do not automatically publish the same local `ProfileChanged` event as self-service profile operations.

Refresh affected administrative UI data explicitly when needed.

## 16. Understanding User Status

UltimateAuth separates account lifecycle status from profile information.

A user may be active, suspended, disabled, locked, awaiting activation, or in another supported lifecycle state.

Status can affect authentication eligibility, session continuation, and authorization.

However, the exact enforcement behavior depends on the configured server policies and authentication mode.

### Self-Service and Administrative Statuses

UltimateAuth exposes separate status enums for self-service and administrative operations.

This prevents the client contract from treating every administrative lifecycle transition as a self-service capability.

**SelfAssignableUserStatus**

```csharp
public enum SelfAssignableUserStatus
{
    Active = 0,
    SelfSuspended = 10
}
```

**AdminAssignableUserStatus**

```csharp
public enum AdminAssignableUserStatus
{
    Active = 0,
    Disabled = 20,
    Suspended = 30,
    Locked = 40,
    RiskHold = 50,
    PendingActivation = 60,
    PendingVerification = 70,
    Unknown = 100
}
```

These enums describe requestable statuses.

They do not mean every transition is permitted in every application context.

The server validates the requested transition.

## 17. Change Your Own Status

Use `ChangeMyStatusAsync` for supported self-service status transitions.

```csharp
var result = await UAuthClient.Users.ChangeMyStatusAsync(
    new ChangeUserStatusSelfRequest
    {
        NewStatus = SelfAssignableUserStatus.SelfSuspended
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } change)
{
    Console.WriteLine($"New status: {change.CurrentStatus}");
}
```

This operation can be used to implement account self-suspension.

The server determines whether the transition is permitted.

### UserStatusChangeResult

The result model includes:

| Property | Description |
|---|---|
| `IsSuccess` | Whether the status operation succeeded |
| `PreviousStatus` | Status before the operation |
| `CurrentStatus` | Status after the operation |
| `FailureReason` | Optional failure description |

For example:

```csharp
if (result.IsSuccess && result.Value is { } change)
{
    if (change.IsSuccess)
    {
        Console.WriteLine($"{change.PreviousStatus} -> {change.CurrentStatus}");
    }
    else
    {
        Console.WriteLine(change.FailureReason ?? "Status change failed.");
    }
}
```

The result may also report a successful no-change outcome when the requested state already matches the current state.

### State Event

After a successful SDK operation, `ChangeMyStatusAsync` publishes `ProfileChanged`.

This is the current client event behavior, even though the operation changes account status rather than editable profile fields.

Applications should not assume that a status change always produces a dedicated `AuthorizationChanged` event.

## 18. Change Another User's Status

Use `ChangeUserStatusAsync` for administrative lifecycle management.

```csharp
var result = await UAuthClient.Users.ChangeUserStatusAsync(userKey,
    new ChangeUserStatusAdminRequest
    {
        NewStatus = AdminAssignableUserStatus.Suspended
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } change)
{
    Console.WriteLine($"Current status: {change.CurrentStatus}");
}
```

This operation requires administrative authorization.

### Example: Disable an Account

```csharp
var result = await UAuthClient.Users.ChangeUserStatusAsync(userKey,
    new ChangeUserStatusAdminRequest
    {
        NewStatus = AdminAssignableUserStatus.Disabled
    });
```

### Example: Restore an Active Status

```csharp
var result = await UAuthClient.Users.ChangeUserStatusAsync(userKey,
    new ChangeUserStatusAdminRequest
    {
        NewStatus = AdminAssignableUserStatus.Active
    });
```

Whether a particular transition is permitted depends on the server's lifecycle rules.

Do not assume that setting `Active` automatically restores every form of previously revoked authentication state.

## 19. Delete the Current User

Use `DeleteMeAsync` to request deletion of the current account.

```csharp
var result = await UAuthClient.Users.DeleteMeAsync();

if (result.IsSuccess)
{
    // Account deletion succeeded.
}
```

Self-service deletion is a soft-delete operation.

According to the `IUserClient` contract, this operation removes associated profiles and sessions.

After successful deletion, the client publishes a `UserDeleted` state event using patch handling mode.

### Important Considerations

Account deletion is not equivalent to ordinary logout.

It changes the user's lifecycle state and may affect associated data across multiple domains.

Before exposing a delete-account action, applications should provide an appropriate confirmation flow and explain the consequences.

The server remains responsible for authorization, data cleanup, and deletion policy enforcement.

## 20. Administrative User Deletion

Administrators can delete users through `DeleteUserAsync`.

The method accepts a `DeleteUserRequest` specifying the deletion mode.

### Soft Delete

```csharp
var result = await UAuthClient.Users.DeleteUserAsync(userKey,
    new DeleteUserRequest
    {
        Mode = DeleteMode.Soft
    });

if (result.IsSuccess && result.Value is { IsSuccess: true } deleted)
{
    Console.WriteLine($"Deletion mode: {deleted.Mode}");
}
```

### Hard Delete

```csharp
var result = await UAuthClient.Users.DeleteUserAsync(userKey,
    new DeleteUserRequest
    {
        Mode = DeleteMode.Hard
    });
```

Hard deletion is a more destructive operation and should be restricted to appropriately authorized administrative workflows.

### DeleteMode

```csharp
public enum DeleteMode
{
    Soft = 0,
    Hard = 10
}
```

### UserDeleteResult

The operation returns `UAuthResult<UserDeleteResult>`.

`UserDeleteResult` includes:

- `IsSuccess`
- `Mode`
- `FailureReason`

For example:

```csharp
if (!result.IsSuccess)
{
    // Handle transport or API-level failure.
}
else if (result.Value is { IsSuccess: false } deletion)
{
    // Handle the operation-level failure.
    Console.WriteLine(deletion.FailureReason);
}
```

A previously deleted account may produce an operation-level success result indicating that no additional deletion was necessary.

Do not infer the complete persistence or retention behavior from the `DeleteMode` value alone.

## 21. Build a User Administration Table

The following example demonstrates a basic Blazor user list using `QueryAsync`.

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Core.Contracts
@using CodeBeam.UltimateAuth.Users.Contracts
@inject IUAuthClient UAuthClient

<h3>Users</h3>

<input @bind="_search" placeholder="Search users" />
<button @onclick="SearchAsync">Search</button>

@if (_loading)
{
    <p>Loading users...</p>
}
else if (_error is not null)
{
    <p role="alert">@_error</p>
}
else
{
    <table>
        <thead>
            <tr>
                <th>Name</th>
                <th>Username</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var user in _users)
            {
                <tr @key="user.UserKey">
                    <td>@user.DisplayName</td>
                    <td>@user.UserName</td>
                    <td>@user.Status</td>
                </tr>
            }
        </tbody>
    </table>

    <button @onclick="PreviousPageAsync" disabled="@(_pageNumber <= 1)">Previous</button>
    <button @onclick="NextPageAsync" disabled="@(!_hasNext)">Next</button>
}

@code {
    private string _search = "";
    private int _pageNumber = 1;
    private bool _hasNext;
    private bool _loading;
    private string? _error;
    private IReadOnlyList<UserSummary> _users = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task SearchAsync()
    {
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (_pageNumber <= 1)
            return;

        _pageNumber--;
        await LoadAsync();
    }

    private async Task NextPageAsync()
    {
        if (!_hasNext)
            return;

        _pageNumber++;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var result = await UAuthClient.Users.QueryAsync(
                new UserQuery
                {
                    Search = _search,
                    PageNumber = _pageNumber,
                    PageSize = 20
                });

            if (result.IsSuccess && result.Value is { } page)
            {
                _users = page.Items;
                _hasNext = page.HasNext;
            }
            else
            {
                _error = "Unable to load users.";
                _users = [];
                _hasNext = false;
            }
        }
        catch (Exception)
        {
            _error = "The user service is unavailable.";
            _users = [];
            _hasNext = false;
        }
        finally
        {
            _loading = false;
        }
    }
}
```

This example demonstrates searching and pagination.

A production administration interface should additionally include loading indicators, action confirmation, permission-aware UI controls, and appropriate handling for concurrent changes.

All administrative operations must remain protected by server-side authorization.

## 22. Handling User Operation Results

UltimateAuth uses structured result contracts for user operations.

Some methods return:

```csharp
UAuthResult
```

Others return:

```csharp
UAuthResult<T>
```

For operations such as user creation, status changes, and deletion, the returned value also contains an operation-level `IsSuccess` property.

### Transport-Level Versus Operation-Level Success

```csharp
var result = await UAuthClient.Users.CreateAsync(request);

if (!result.IsSuccess)
{
    // API-level failure.
}
else if (result.Value is { IsSuccess: false } operation)
{
    // User creation was processed but did not succeed.
    Console.WriteLine(operation.FailureReason);
}
else if (result.Value is { IsSuccess: true } created)
{
    Console.WriteLine(created.UserKey);
}
```

Check the appropriate success level for the operation.

### HTTP Status Helpers

`UAuthResult` exposes convenient status helpers.

```csharp
if (result.Http.IsUnauthorized)
{
    // Authentication required.
}
else if (result.Http.IsForbidden)
{
    // Insufficient permission.
}
else if (result.Http.IsConflict)
{
    // Conflicting operation or state.
}
else if (result.Http.IsBadRequest)
{
    // Invalid request.
}
```

Additional diagnostic fields include `CorrelationId`, `TraceId`, `Problem`, and `ErrorText`.

Avoid exposing sensitive internal failure details directly to end users.

## 23. Client State Events

`UAuthUserClient` publishes state events for successful self-service operations.

| Operation | Event |
|---|---|
| `UpdateMyProfileAsync` | `ProfileChanged` |
| `CreateMyProfileAsync` | `ProfileChanged` |
| `DeleteMyProfileAsync` | `ProfileChanged` |
| `ChangeMyStatusAsync` | `ProfileChanged` |
| `DeleteMeAsync` | `UserDeleted` |

The event payload depends on the operation.

Administrative user operations do not automatically publish these same local self-service events.

For example, after updating another user's profile, an administration panel should refresh the affected user data as needed.

Client events help synchronize application state, but they do not replace authoritative server-side account status or permission checks.

## 24. Security and Lifecycle Considerations

### Self-Service and Administrative Boundaries

Self-service methods operate on the current authenticated user.

Administrative methods identify an explicit target user.

Do not assume that access to the SDK automatically authorizes an operation.

The server must enforce actor, target, tenant, and action requirements.

### Status Changes and Existing Sessions

Changing account status may affect authentication eligibility and session continuation.

However, immediate invalidation of existing credentials depends on the server's security policy and authentication mode.

A client-side status update is not itself proof that every previously issued token has been invalidated.

### Profile Data Is Not Authorization Data

A user's display name, biography, or profile metadata should not be used as proof of permissions.

Authorization decisions must use the relevant trusted security context.

### Profile Replacement Can Cause Data Loss

`UpdateProfileRequest` is not a patch contract.

Submitting an incomplete request may clear existing profile fields.

Applications should preserve intended values and consider concurrency when multiple editors can modify the same profile.

### Deletion Is a Lifecycle Operation

Soft deletion and hard deletion have different implications.

Applications should consider retention, recovery, audit requirements, and dependent data before exposing destructive actions.

## Next Steps

- [Identifiers](/docs/client/identifiers) — Manage usernames, email addresses, phone numbers, and verification.
- [Authentication](/docs/client/authentication) — Login, refresh, logout, and authentication state.
- [Session Management](/docs/client/session-management) — Inspect and revoke sessions across devices.
- [Users](/docs/plugin-domains/users) — Understand the user domain and lifecycle architecture.
- [Policies & Access Control](/docs/plugin-domains/policies) — Understand authorization enforcement for user operations.