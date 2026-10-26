---
title: Authorization
order: 7
group: client
---

# Authorization

UltimateAuth provides a unified client API for managing roles, assigning permissions, controlling user-role relationships, and requesting authorization decisions from the server.

Through `UAuthClient.Authorization`, applications can build administrative authorization interfaces and check whether the current user is allowed to perform a particular action.

Authorization is enforced on the server. Client-side permission checks are useful for improving the user experience, but they do not replace authorization at the endpoint or application-service level.

This guide covers the current `IAuthorizationClient` API, practical role management workflows, permission patterns, and secure client integration.

## 1. Understanding Authorization

UltimateAuth authorization combines several concepts:

| Concept | Purpose |
|---|---|
| Roles | Named collections of permissions |
| Permissions | Fine-grained capabilities represented by normalized values |
| User-role assignments | Associations between users and roles |
| Policies | Server-side rules that participate in access decisions |
| Authorization context | Information about the actor, resource, action, and current security state |

A typical authorization relationship looks like this:

```text
User
  |
  +-- Manager
  |     |
  |     +-- products.read
  |     +-- products.create
  |     +-- products.update
  |
  +-- Auditor
        |
        +-- reports.read
```

A user can have multiple roles, and each role can contain multiple permissions.

However, **having a permission does not automatically guarantee access**. The server's authorization pipeline may apply additional requirements, such as resource ownership, session validity, tenant isolation, or application policies.

For the internal decision model, see [Authorization Pipeline Internals](/docs/security/authorization-pipeline).

## 2. Accessing the Authorization Client

Inject `IUAuthClient` into a Blazor component:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Authorization operations are available through:

```csharp
UAuthClient.Authorization
```

This property exposes `IAuthorizationClient`.

The interface provides three main groups of operations:

- **Authorization checks:** Evaluate whether the current user may perform an action.
- **Role management:** Create, query, rename, configure, and delete roles.
- **User-role management:** Retrieve, assign, and remove user roles.

The SDK sends requests to the corresponding UltimateAuth endpoints and returns structured `UAuthResult` values.

## 3. Understanding Permissions

A permission is represented by the `Permission` value type.

```csharp
var permission = Permission.From("products.create");
```

The current implementation normalizes permission values by trimming surrounding whitespace and converting the value to lowercase.

For example:

```csharp
var permission = Permission.From("  PRODUCTS.CREATE  ");

Console.WriteLine(permission.Value);
// products.create
```

The constructor rejects null, empty, and whitespace-only values.

### Permission Patterns

UltimateAuth recognizes three important forms:

| Pattern | Example | Meaning |
|---|---|---|
| Exact | `products.create` | A specific permission |
| Prefix wildcard | `products.*` | Permissions within the corresponding prefix |
| Global wildcard | `*` | Unrestricted permission matching within the applicable authorization rules |

Create them using:

```csharp
var exact = Permission.From("products.create");

var prefix = Permission.From("products.*");

var global = Permission.Wildcard;
```

The type also exposes:

```csharp
permission.IsWildcard;
permission.IsPrefix;
permission.IsValid;
```

### Permission Normalization

The following values normalize to the same representation:

```csharp
Permission.From("Products.Create");
Permission.From("products.create");
Permission.From(" PRODUCTS.CREATE ");
```

All produce:

```text
products.create
```

Use consistent permission names across application services, policies, and role configuration.

### Wildcards Are Powerful

For example:

```csharp
Permission.From("users.*")
```

may grant broad access to operations within the `users` permission prefix.

The global wildcard:

```csharp
Permission.Wildcard
```

is especially sensitive.

Avoid assigning broad wildcard permissions merely to simplify development. Use least-privilege permissions for ordinary application roles.

A wildcard is not a substitute for satisfying independent security policies, authentication requirements, or tenant restrictions.

## 4. Check Authorization

`CheckAsync` evaluates whether the current authenticated user is authorized to perform a specific action against a resource.

It provides an **on-demand, server-side authorization decision** based on the current authentication context, permissions, roles, and applicable authorization policies.

Unlike operations that perform protected actions, `CheckAsync` only evaluates access. It does not execute the requested operation, modify permissions, grant access, or reserve authorization for a future request.

### When to Use CheckAsync

Use `CheckAsync` when your application needs to:

- Determine whether an authenticated user can perform an action before presenting it in the UI.
- Enable, disable, or hide controls based on the current authorization decision.
- Explain why an operation is unavailable, using the returned denial reason where appropriate.
- Determine whether an operation requires reauthentication.
- Evaluate resource-specific access when the applicable policies support it.

For example, an application may check whether the current user can delete a particular order before displaying the Delete button.

### What CheckAsync Does Not Guarantee

**A successful authorization check is not a guarantee that a subsequent operation will succeed.**

Authorization is evaluated against the security context available at the time of the check. That context may change before the actual operation is performed.

For example:

- An administrator may revoke the user's permissions.
- The user's session may expire or become invalid.
- The target resource may be deleted or modified.
- The resource's ownership or tenant context may change.
- Additional business rules may prevent the operation.

The protected operation must independently enforce authorization using its current context.

**Never use a previous `CheckAsync` result as proof of authorization for a later request.**

`CheckAsync` is primarily a developer-experience and user-experience API. It helps applications understand and present authorization decisions without replacing the security enforcement performed by protected endpoints and application services.

Use `CheckAsync` to ask the server whether the current user is authorized to perform an action.

```csharp
var result = await UAuthClient.Authorization.CheckAsync(
    new AuthorizationCheckRequest
    {
        Action = "read",
        Resource = "products"
    });

if (result.IsSuccess && result.Value is { IsAllowed: true })
{
    // The server authorized this action.
}
```

The method returns:

```csharp
UAuthResult<AuthorizationResult>
```

### AuthorizationCheckRequest

The request contains:

| Property | Type | Description |
|---|---|---|
| `Action` | `string` | Requested operation |
| `Resource` | `string` | Resource being accessed |
| `ResourceId` | `string?` | Optional identifier for a specific resource instance |

Both `Action` and `Resource` are required.

The action and resource values must follow the conventions understood by your server-side authorization configuration.

### Check a Specific Resource

For example, check access to a particular product:

```csharp
var result = await UAuthClient.Authorization.CheckAsync(
    new AuthorizationCheckRequest
    {
        Action = "update",
        Resource = "products",
        ResourceId = productId.ToString()
    });
```

Providing `ResourceId` allows the server to evaluate resource-specific rules where supported by the configured authorization pipeline.

For example, the application may distinguish between updating any product and updating a product owned by the current user.

Do not assume that sending `ResourceId` automatically enables ownership checking. The relevant server-side policy must implement the required rule.

## 5. Understanding Authorization Results

`CheckAsync` returns an `AuthorizationResult` with the following properties:

| Property | Description |
|---|---|
| `IsAllowed` | Whether the request was authorized |
| `RequiresReauthentication` | Whether the decision indicates reauthentication is required |
| `DenyReason` | Optional denial reason code |

### Authorization Decisions vs. HTTP Results

`CheckAsync` separates the outcome of the API request from the authorization decision itself.

The method returns `UAuthResult<AuthorizationResult>`:

- `UAuthResult.IsSuccess` indicates whether the authorization check request completed successfully.
- `AuthorizationResult.IsAllowed` indicates whether the requested action is currently authorized.
- `AuthorizationResult.RequiresReauthentication` indicates whether additional authentication is required.
- `AuthorizationResult.DenyReason` provides an optional reason for denial.

A denied authorization decision is a valid result of a successful authorization check.

| Scenario | HTTP Status | Authorization Result |
|---|---|---|
| Action allowed | `200 OK` | `IsAllowed = true` |
| Action denied | `200 OK` | `IsAllowed = false` |
| Reauthentication required | `200 OK` | `RequiresReauthentication = true` |
| Caller not authenticated | `401 Unauthorized` | No authorization decision |
| Invalid request | `400 Bad Request` | No authorization decision |
| Server-side evaluation failure | Appropriate error status | No authorization decision |

**HTTP 200 means the authorization evaluation completed successfully. It does not mean access was granted.**

Only authenticated callers can use the current `CheckAsync` endpoint. However, callers do not need permission to perform the action they are checking.

This distinction is intentional: the purpose of `CheckAsync` is to determine whether the action is permitted.

### Handling Denied Decisions

Applications should distinguish between authorization denial, reauthentication requirements, and request failures.

A denied decision may be used to disable an action or display an appropriate message.

A reauthentication requirement may be used to initiate the application's supported reauthentication flow.

A failed request must not be interpreted as permission to proceed.

**Fail closed:** If the authorization decision is unavailable, treat the operation as unavailable in the UI and rely on server-side authorization when the actual operation is requested.

### Handle All Decision Outcomes

```csharp
var result = await UAuthClient.Authorization.CheckAsync(
    new AuthorizationCheckRequest
    {
        Action = "delete",
        Resource = "products",
        ResourceId = productId.ToString()
    });

if (!result.IsSuccess)
{
    // The authorization request itself failed.
}
else if (result.Value is { RequiresReauthentication: true })
{
    // Ask the user to authenticate again.
}
else if (result.Value is { IsAllowed: true })
{
    // The operation is allowed.
}
else
{
    // The operation was denied.
    var reason = result.Value?.DenyReason;
}
```

An unsuccessful API request and an explicit authorization denial are different outcomes.

For example:

- `result.IsSuccess == false` means the authorization request did not complete successfully at the API level.
- `result.Value.IsAllowed == false` means the server returned a denial decision.
- `result.Value.RequiresReauthentication == true` indicates that the caller needs to reestablish authentication.

Treat failed or unavailable authorization checks as **not authorized** in the UI.

Never grant access because an authorization request failed.

## 6. Use Authorization Checks in Blazor

A common use case is controlling whether an action button should be displayed.

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Authorization.Contracts
@inject IUAuthClient UAuthClient

@if (_canCreate)
{
    <button @onclick="CreateProductAsync">Create Product</button>
}

@code {
    private bool _canCreate;

    protected override async Task OnInitializedAsync()
    {
        var result = await UAuthClient.Authorization.CheckAsync(
            new AuthorizationCheckRequest
            {
                Action = "create",
                Resource = "products"
            });

        _canCreate = result.IsSuccess && result.Value is { IsAllowed: true };
    }

    private Task CreateProductAsync()
    {
        // Invoke the protected application operation.
        return Task.CompletedTask;
    }
}
```

This improves the interface by hiding unavailable actions.

However, the endpoint handling product creation must still enforce authorization independently.

### Why Client Checks Are Not Security Boundaries

A user can bypass a hidden button and send a request directly to an endpoint.

Therefore:

```text
Client authorization check
          |
          v
Improve the user experience
          |
          v
User submits an operation
          |
          v
Server independently authorizes the operation
          |
          v
Execute or deny
```

Client authorization checks should never be the only protection for sensitive operations.

## 7. Get My Roles

Use `GetMyRolesAsync` to retrieve roles assigned to the current authenticated user.

```csharp
var result = await UAuthClient.Authorization.GetMyRolesAsync();

if (result.IsSuccess && result.Value is { } data)
{
    foreach (var role in data.Roles.Items)
    {
        Console.WriteLine(role.Name);
    }
}
```

The method returns:

```csharp
UAuthResult<UserRolesGetResult>
```

### UserRolesGetResult

The response contains:

| Property | Description |
|---|---|
| `UserKey` | User whose roles were requested |
| `Roles` | Paginated collection of role assignments |

Each entry is a `UserRoleInfo`.

### UserRoleInfo

| Property | Description |
|---|---|
| `Tenant` | Tenant associated with the assignment |
| `UserKey` | Assigned user |
| `RoleId` | Assigned role |
| `Name` | Role name |
| `AssignedAt` | Assignment timestamp |

### Pagination

```csharp
var result = await UAuthClient.Authorization.GetMyRolesAsync(
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 20
    });
```

The returned role collection is paginated.

Do not assume that the first page contains all assigned roles.

## 8. Get My Permissions

Use `GetMyPermissionsAsync()` to retrieve the permissions assigned to the currently authenticated user through their roles.

Unlike `GetMyRolesAsync()`, which returns role assignments, this method returns the resulting permission collection.

```csharp
var result = await UAuthClient.Authorization.GetMyPermissionsAsync();

if (result.IsSuccess && result.Value is { } data)
{
    foreach (var permission in data.Permissions)
    {
        Console.WriteLine(permission.Value);
    }
}
```

The method is self-service and does not require administrative permission.

The user identity and tenant context are derived from the authenticated session. No user identifier or tenant identifier is accepted as a request parameter.

### UserPermissionsResult

The response contains the user's role-derived permissions:

```csharp
public sealed record UserPermissionsResult
{
    public required IReadOnlyList<Permission> Permissions { get; init; }
}
```

The returned collection:

- Combines permissions from the user's assigned roles.
- Removes duplicate permissions.
- Uses deterministic ordering.
- Preserves wildcard permissions such as `users.*` and `*`.
- Returns an empty collection when no permissions are assigned.
- Reflects the current permissions resolved from the configured authorization stores.

For example, a user assigned multiple roles may receive:

```text
reports.*
users.read.admin
users.update.admin
```

The result does not identify which role contributed each permission.

### Assigned Permissions vs. Authorization Decisions

**Assigned permissions are not final authorization decisions.**

A user may possess a permission but still be denied access because of an applicable policy, tenant boundary, target restriction, or other security requirement.

For example:

```csharp
var permissions = await UAuthClient.Authorization.GetMyPermissionsAsync();
```

This retrieves the user's assigned permissions.

To determine whether a specific operation is currently authorized, use `CheckAsync()`:

```csharp
var result = await UAuthClient.Authorization.CheckAsync(
    new AuthorizationCheckRequest
    {
        Action = "users.delete.admin",
        Resource = "users",
        ResourceId = targetUserKey.Value
    });

if (result.IsSuccess && result.Value is { } decision)
{
    if (decision.IsAllowed)
    {
        // The authorization check allowed this operation.
    }
}
```

Even a successful client-side authorization check does not replace authorization enforcement at the server-side operation boundary.

### Wildcard Permissions

Wildcard permissions are returned without expansion.

For example:

```text
users.*
```

remains `users.*` rather than being expanded into individual user-management actions.

This preserves the stored permission semantics and avoids depending on a potentially incomplete action catalog.

### Security Considerations

`GetMyPermissionsAsync()` is intended for permission inspection, user interfaces, and administrative transparency.

It must not be used as the sole mechanism for protecting sensitive operations.

The server remains responsible for evaluating authorization policies and enforcing access at the actual execution boundary.

See [Check Authorization](/docs/client-sdk/authorization#4-check-authorization) for policy-aware authorization checks.

## 9. Get Another User's Roles

Administrative interfaces can retrieve the roles assigned to a specific user.

```csharp
var result = await UAuthClient.Authorization.GetUserRolesAsync(userKey,
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 20
    });

if (result.IsSuccess && result.Value is { } data)
{
    foreach (var role in data.Roles.Items)
    {
        Console.WriteLine($"{role.Name} — {role.AssignedAt}");
    }
}
```

This operation requires appropriate administrative authorization.

The `UserKey` identifies the target user, not the current actor.

The server determines whether the current actor may inspect that user's role assignments.

## 10. Query Roles

Use `QueryRolesAsync` to retrieve roles with pagination and optional filtering.

```csharp
var result = await UAuthClient.Authorization.QueryRolesAsync(
    new RoleQuery
    {
        PageNumber = 1,
        PageSize = 20
    });

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var role in page.Items)
    {
        Console.WriteLine(role.Name);
    }
}
```

The method returns:

```csharp
UAuthResult<PagedResult<RoleInfo>>
```

### RoleQuery

`RoleQuery` inherits from `PageRequest` and adds:

| Property | Description |
|---|---|
| `Search` | Optional role-name search |
| `IncludeDeleted` | Include deleted roles when supported and authorized |
| `PageNumber` | Requested page |
| `PageSize` | Requested page size |

### Search Roles

```csharp
var result = await UAuthClient.Authorization.QueryRolesAsync(
    new RoleQuery
    {
        Search = "manager",
        PageNumber = 1,
        PageSize = 10
    });
```

### Include Deleted Roles

```csharp
var result = await UAuthClient.Authorization.QueryRolesAsync(
    new RoleQuery
    {
        IncludeDeleted = true,
        PageNumber = 1,
        PageSize = 20
    });
```

Use `IncludeDeleted` only where the application needs administrative access to deleted role records.

The server remains responsible for enforcing the query's permissions and lifecycle rules.

## 11. Understanding RoleInfo

Each role returned by the SDK is represented by `RoleInfo`.

| Property | Type | Description |
|---|---|---|
| `Id` | `RoleId` | Role identifier |
| `Name` | `string` | Role name |
| `Permissions` | `IReadOnlyCollection<Permission>` | Assigned permissions |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp |
| `UpdatedAt` | `DateTimeOffset?` | Last update timestamp |

For example:

```csharp
var result = await UAuthClient.Authorization.QueryRolesAsync(
    new RoleQuery
    {
        PageNumber = 1,
        PageSize = 20
    });

if (result.IsSuccess && result.Value is { } page)
{
    foreach (var role in page.Items)
    {
        Console.WriteLine($"Role: {role.Name}");

        foreach (var permission in role.Permissions)
        {
            Console.WriteLine($"  {permission.Value}");
        }
    }
}
```

`RoleId` is a strongly typed identifier rather than a plain `Guid`.

For example:

```csharp
RoleId roleId = RoleId.From(existingGuid);
```

Use `RoleId` for role management requests.

Do not confuse it with `UserKey` or other identifier types.

## 12. Create a Role

Use `CreateRoleAsync` to create a role.

```csharp
var result = await UAuthClient.Authorization.CreateRoleAsync(
    new CreateRoleRequest
    {
        Name = "Manager"
    });

if (result.IsSuccess && result.Value is { } role)
{
    Console.WriteLine($"Created: {role.Name}");
    Console.WriteLine($"Role ID: {role.Id}");
}
```

The method returns:

```csharp
UAuthResult<RoleInfo>
```

Role names must be unique within the relevant tenant according to the server's role-name rules.

### Create a Role with Permissions

`CreateRoleRequest` supports an optional initial permission collection.

```csharp
var result = await UAuthClient.Authorization.CreateRoleAsync(
    new CreateRoleRequest
    {
        Name = "ProductManager",
        Permissions =
        [
            Permission.From("products.read"),
            Permission.From("products.create"),
            Permission.From("products.update")
        ]
    });
```

This allows applications to create and configure a role in one operation.

The server validates the requested permissions and whether the caller is authorized to create the role.

### Create a Read-Only Role

```csharp
var result = await UAuthClient.Authorization.CreateRoleAsync(
    new CreateRoleRequest
    {
        Name = "Auditor",
        Permissions =
        [
            Permission.From("reports.read"),
            Permission.From("products.read")
        ]
    });
```

Use role names that describe the responsibility or capability represented by the role.

## 13. Rename a Role

Use `RenameRoleAsync` to change a role's name.

```csharp
var result = await UAuthClient.Authorization.RenameRoleAsync(
    new RenameRoleRequest
    {
        Id = roleId,
        Name = "SeniorManager"
    });

if (result.IsSuccess)
{
    // Role renamed.
}
```

`RenameRoleRequest` requires:

- `Id`: The role's `RoleId`.
- `Name`: The new role name.

The operation may fail if another role already uses the requested name.

Role renaming does not mean that permissions are automatically changed.

After a successful rename, the SDK publishes an `AuthorizationChanged` state event.

## 14. Set Role Permissions

Use `SetRolePermissionsAsync` to configure a role's permissions.

```csharp
var result = await UAuthClient.Authorization.SetRolePermissionsAsync(
    new SetRolePermissionsRequest
    {
        RoleId = roleId,
        Permissions =
        [
            Permission.From("products.read"),
            Permission.From("products.create"),
            Permission.From("products.update")
        ]
    });
```

The request contains:

| Property | Description |
|---|---|
| `RoleId` | Role to modify |
| `Permissions` | New permission collection |

### Important: This Is a Replacement Operation

`SetRolePermissionsAsync` replaces the role's existing permission collection.

It does not append permissions to the current collection.

For example, suppose a role currently contains:

```text
products.read
products.create
products.update
```

Submitting:

```csharp
Permissions =
[
    Permission.From("products.read")
]
```

means the intended new permission collection contains only:

```text
products.read
```

The other permissions are removed from the role.

### Safely Add a Permission to an Existing Role

To preserve existing permissions, first retrieve the role and construct the complete desired collection.

```csharp
var query = await UAuthClient.Authorization.QueryRolesAsync(
    new RoleQuery
    {
        Search = "ProductManager",
        PageNumber = 1,
        PageSize = 50
    });

if (query.IsSuccess && query.Value is { } page)
{
    var role = page.Items.FirstOrDefault(x => x.Name == "ProductManager");

    if (role is not null)
    {
        var permissions = role.Permissions.Append(Permission.From("products.delete")).Distinct().ToArray();

        var result =
            await UAuthClient.Authorization.SetRolePermissionsAsync(
                new SetRolePermissionsRequest
                {
                    RoleId = role.Id,
                    Permissions = permissions
                });
    }
}
```

This is a read-modify-write pattern, not an atomic append operation.

If multiple administrators edit the same role concurrently, the last successful replacement may overwrite another administrator's changes.

Applications requiring concurrency-safe editing should implement suitable coordination or version checking at the application level.

### Remove a Permission

```csharp
var updatedPermissions = role.Permissions.Where(x => x.Value != "products.delete").ToArray();

var result = await UAuthClient.Authorization.SetRolePermissionsAsync(
    new SetRolePermissionsRequest
    {
        RoleId = role.Id,
        Permissions = updatedPermissions
    });
```

Again, submit the complete desired permission set.

### Clear All Permissions

```csharp
var result = await UAuthClient.Authorization.SetRolePermissionsAsync(
    new SetRolePermissionsRequest
    {
        RoleId = roleId,
        Permissions = []
    });
```

An empty collection requests that the role have no permissions.

The server determines whether the operation is permitted.

## 15. Assign a Role to a User

Use `AssignRoleToUserAsync` to assign an existing role to a user.

```csharp
var result = await UAuthClient.Authorization.AssignRoleToUserAsync(
    new AssignRoleRequest
    {
        UserKey = userKey,
        RoleName = "Manager"
    });

if (result.IsSuccess)
{
    // Role assigned.
}
```

The request contains:

| Property | Description |
|---|---|
| `UserKey` | Target user |
| `RoleName` | Existing role name |

The role must exist, and the current actor must be authorized to assign it.

### Assign Multiple Roles

```csharp
foreach (var roleName in new[] { "Manager", "Auditor" })
{
    var result = await UAuthClient.Authorization.AssignRoleToUserAsync(
        new AssignRoleRequest
        {
            UserKey = userKey,
            RoleName = roleName
        });

    if (!result.IsSuccess)
    {
        // Handle the failed assignment.
        break;
    }
}
```

Each call is a separate operation.

This example does not provide transactional, all-or-nothing behavior across the entire collection.

For complex provisioning workflows, consider whether the application needs a server-side orchestration operation.

## 16. Remove a Role from a User

Use `RemoveRoleFromUserAsync`:

```csharp
var result = await UAuthClient.Authorization.RemoveRoleFromUserAsync(
    new RemoveRoleRequest
    {
        UserKey = userKey,
        RoleName = "Manager"
    });

if (result.IsSuccess)
{
    // Role removed.
}
```

Removing a role changes the user's role assignments.

It does not delete the role itself.

Other users assigned to that role are unaffected by this particular operation.

### Important: Effective Permissions May Remain

A user may receive the same permission through multiple roles.

For example:

```text
User
  |
  +-- Manager
  |     +-- products.read
  |
  +-- Auditor
        +-- products.read
```

Removing `Manager` does not necessarily remove the user's ability to read products because `Auditor` still provides `products.read`.

To determine whether the user may perform an action, rely on the server's authorization decision rather than inspecting one role assignment in isolation.

## 17. Delete a Role

Use `DeleteRoleAsync` to delete a role.

```csharp
var result = await UAuthClient.Authorization.DeleteRoleAsync(
    new DeleteRoleRequest
    {
        Id = roleId,
        Mode = DeleteMode.Soft
    });
```

The request supports a deletion mode.

### Hard Delete

```csharp
var result = await UAuthClient.Authorization.DeleteRoleAsync(
    new DeleteRoleRequest
    {
        Id = roleId,
        Mode = DeleteMode.Hard
    });
```

The availability and effect of each deletion mode are determined by server-side lifecycle rules.

### RoleDeleteResult

The method returns:

```csharp
UAuthResult<RoleDeleteResult>
```

The response contains:

| Property | Description |
|---|---|
| `RoleId` | Deleted role |
| `RemovedAssignments` | Number of user-role assignments removed |
| `Mode` | Applied deletion mode |
| `DeletedAt` | Deletion timestamp |

### Inspect the Result

```csharp
var result = await UAuthClient.Authorization.DeleteRoleAsync(
    new DeleteRoleRequest
    {
        Id = roleId,
        Mode = DeleteMode.Soft
    });

if (result.IsSuccess && result.Value is { } deleted)
{
    Console.WriteLine($"Removed assignments: {deleted.RemovedAssignments}");
}
```

Deleting a role removes its assignments from users.

This can affect multiple users at once.

For that reason, role deletion should be presented as a sensitive administrative operation with appropriate confirmation.

## 18. Build a Role Management Page

The following Blazor example demonstrates querying roles, creating a role, and deleting a selected role.

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Authorization.Contracts
@using CodeBeam.UltimateAuth.Core.Contracts
@inject IUAuthClient UAuthClient

<h3>Role Management</h3>

<input @bind="_search" placeholder="Search roles" />

<button @onclick="LoadRolesAsync">Search</button>

@if (_loading)
{
    <p>Loading roles...</p>
}
else
{
    @foreach (var role in _roles)
    {
        <div @key="role.Id">
            <strong>@role.Name</strong>
            <span>@role.Permissions.Count permissions</span>
            <button @onclick="() => DeleteRoleAsync(role.Id)">Delete</button>
        </div>
    }
}

<h4>Create Role</h4>
<input @bind="_newRoleName" placeholder="Role name" />
<button @onclick="CreateRoleAsync" disabled="@_busy">Create</button>

@if (_message is not null)
{
    <p role="status">@_message</p>
}

@code {
    private IReadOnlyList<RoleInfo> _roles = [];

    private string _search = "";
    private string _newRoleName = "";
    private string? _message;

    private bool _loading = true;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        _loading = true;

        try
        {
            var result = await UAuthClient.Authorization.QueryRolesAsync(
                new RoleQuery
                {
                    Search = _search,
                    PageNumber = 1,
                    PageSize = 50
                });

            if (result.IsSuccess && result.Value is { } page)
            {
                _roles = page.Items;
            }
            else
            {
                _message = "Unable to load roles.";
            }
        }
        catch (Exception)
        {
            _message = "The authorization service is unavailable.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CreateRoleAsync()
    {
        if (_busy || string.IsNullOrWhiteSpace(_newRoleName))
            return;

        _busy = true;
        _message = null;

        try
        {
            var result = await UAuthClient.Authorization.CreateRoleAsync(
                new CreateRoleRequest
                {
                    Name = _newRoleName
                });

            if (result.IsSuccess)
            {
                _newRoleName = "";
                await LoadRolesAsync();
            }
            else
            {
                _message = "Unable to create role.";
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteRoleAsync(RoleId roleId)
    {
        // Add a confirmation dialog before deletion in production.

        var result = await UAuthClient.Authorization.DeleteRoleAsync(
            new DeleteRoleRequest
            {
                Id = roleId,
                Mode = DeleteMode.Soft
            });

        if (result.IsSuccess)
        {
            await LoadRolesAsync();
        }
        else
        {
            _message = "Unable to delete role.";
        }
    }
}
```

This example demonstrates the SDK integration rather than a complete production administration interface.

A production implementation should include pagination, role assignment controls, permission editing, deletion confirmation, and appropriate loading and error handling for every operation.

## 19. Build a User Role Assignment Interface

An administrative user details page can display assigned roles and allow administrators to add or remove them.

### Load User Roles

```csharp
var result = await UAuthClient.Authorization.GetUserRolesAsync(userKey,
    new PageRequest
    {
        PageNumber = 1,
        PageSize = 50
    });

if (result.IsSuccess && result.Value is { } data)
{
    var assignedRoles = data.Roles.Items.ToList();
}
```

### Assign a Selected Role

```csharp
var result = await UAuthClient.Authorization.AssignRoleToUserAsync(
    new AssignRoleRequest
    {
        UserKey = userKey,
        RoleName = selectedRoleName
    });

if (result.IsSuccess)
{
    // Reload the user's assigned roles.
}
```

### Remove a Selected Role

```csharp
var result = await UAuthClient.Authorization.RemoveRoleFromUserAsync(
    new RemoveRoleRequest
    {
        UserKey = userKey,
        RoleName = selectedRoleName
    });

if (result.IsSuccess)
{
    // Reload the user's assigned roles.
}
```

This pattern works well alongside `UAuthClient.Users` for building a unified user administration interface.

See [User Management](/docs/client/user-management).

## 20. Authorization State Events

The current `UAuthAuthorizationClient` publishes:

```csharp
UAuthStateEvent.AuthorizationChanged
```

after successful authorization mutations.

| Operation | Publishes `AuthorizationChanged` |
|---|---|
| `AssignRoleToUserAsync` | Yes |
| `RemoveRoleFromUserAsync` | Yes |
| `RenameRoleAsync` | Yes |
| `SetRolePermissionsAsync` | Yes |
| `DeleteRoleAsync` | Yes |
| `CreateRoleAsync` | No |
| `QueryRolesAsync` | No |
| `GetMyRolesAsync` | No |
| `GetUserRolesAsync` | No |
| `CheckAsync` | No |

The event follows the configured client state event handling mode.

It can be used to refresh navigation menus, role assignment views, or permission-sensitive UI.

However, it is a **local client event**, not a guarantee that all active clients or distributed caches have immediately observed the change.

For example, an administrator assigning a role to another user does not automatically mean that user's open browser receives the administrator's local event.

Applications requiring cross-client updates should provide an appropriate synchronization mechanism.

## 21. Authorization Changes and Active Sessions

Role and permission changes may affect active users.

However, the timing of those changes depends on how authorization data is resolved and cached by the server.

For example:

1. An administrator removes a role.
2. The server persists the new role assignment state.
3. A subsequent authorization evaluation uses the effective authorization state available to the server.

If the application caches authorization information, changes may not become visible everywhere immediately.

Do not assume that a client event invalidates every active session or refreshes every distributed authorization cache.

For sensitive operations, authorization should be evaluated against sufficiently current server-side state.

### Roles Are Not Sessions

Removing a role and revoking a session are different operations.

Removing a role changes authorization data.

Revoking a session changes whether an authentication session remains valid.

Applications may combine these operations when security policy requires immediate termination of access, but the concepts should remain separate.

See [Session Management](/docs/client/session-management).

## 22. Multi-Tenant Authorization

UltimateAuth authorization operations are scoped to the current tenant context.

Role names are unique within the relevant tenant, and role assignments belong to the applicable tenant.

For example:

```text
Tenant A
  |
  +-- Manager
  |     +-- products.read
  |
  +-- User A

Tenant B
  |
  +-- Manager
  |     +-- reports.read
  |
  +-- User B
```

The same role name may represent different permission sets in different tenants.

The client does not gain cross-tenant access merely by providing another user's `UserKey`.

The server must enforce tenant isolation and administrative authorization.

When implementing multi-tenant administration, do not assume role names or permission configurations are globally shared.

## 23. Handling Authorization Errors

Authorization methods return `UAuthResult` or `UAuthResult<T>`.

### Handle Role Creation Errors

```csharp
var result = await UAuthClient.Authorization.CreateRoleAsync(
    new CreateRoleRequest
    {
        Name = "Manager"
    });

if (result.IsSuccess)
{
    // Role created.
}
else if (result.Http.IsConflict)
{
    // A conflicting role may already exist.
}
else if (result.Http.IsForbidden)
{
    // Caller lacks permission.
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

### Handle Authorization Check Failures

```csharp
var result = await UAuthClient.Authorization.CheckAsync(
    new AuthorizationCheckRequest
    {
        Action = "update",
        Resource = "products"
    });

var allowed = result.IsSuccess && result.Value is { IsAllowed: true };
```

This fail-closed pattern is appropriate when determining whether the UI should expose a protected operation.

Use the server's structured error information for diagnostics where appropriate.

Do not expose sensitive policy internals unnecessarily.

## 24. Security Considerations

### Server Authorization Is Authoritative

Client-side checks improve usability but cannot protect an endpoint by themselves.

Every protected operation must enforce authorization on the server.

### Follow Least Privilege

Assign only the permissions a role actually needs.

Avoid using `*` or broad prefix wildcards for ordinary users.

### Protect Administrative Operations

Role creation, permission replacement, role assignment, and role deletion are privileged operations.

The server must authorize each mutation independently.

### Treat Permission Replacement Carefully

`SetRolePermissionsAsync` replaces the entire permission collection.

Always inspect the existing role configuration before submitting changes.

### Handle Reauthentication Requirements

An authorization decision can indicate `RequiresReauthentication`.

Applications should handle this state explicitly rather than treating it as an ordinary permission denial.

### Respect Tenant Boundaries

Roles and assignments must remain within their authorized tenant context.

### Do Not Infer Effective Access from One Role

A user can have multiple roles, overlapping permissions, and additional policy restrictions.

Use the server's authorization decision when determining whether an action is permitted.

### Account for Cached Authorization State

Permission changes may not propagate instantly to every active client or cached authorization context.

Design sensitive operations around server-side enforcement and appropriate cache invalidation.

## 25. Putting Everything Together

A typical administrative authorization workflow may look like this:

```text
Create role
    |
    v
Configure permissions
    |
    v
Assign role to user
    |
    v
User requests protected action
    |
    v
Server evaluates authorization
    |
    +-- Allow --> Execute operation
    |
    +-- Deny --> Reject operation
    |
    +-- Reauthentication required
                  |
                  v
             Authenticate again
```

UltimateAuth separates role management from authorization enforcement.

This allows applications to provide flexible administrative interfaces while keeping access decisions under server control.

## Next Steps

- [Policies & Access Control](/docs/plugin-domains/policies-access-control) — Understand application policies and access rules.
- [Authorization Pipeline Internals](/docs/security/authorization-pipeline-internals) — Explore how authorization decisions are evaluated.
- [User Management](/docs/client/user-management) — Manage user accounts and lifecycle.
- [Session Management](/docs/client/session-management) — Manage authenticated sessions and devices.
- [Session Security Model](/docs/security/session-security) — Understand session validity and security boundaries.