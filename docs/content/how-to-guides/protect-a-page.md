---
title: Protect a Page
order: 2
group: how-to-guides
---

# Protect a Page

Learn how to protect Blazor pages and UI sections using UltimateAuth and ASP.NET Core authorization.

UltimateAuth provides declarative authorization for routed Blazor pages through `[UAuthAuthorize]`, conditional rendering through `UAuthStateView`, and interoperability with ASP.NET Core's `[Authorize]` attribute.

In this guide, you'll learn how to:

- Require authentication before a page can be accessed.
- Restrict access using roles and permissions.
- Combine authorization requirements with different match modes.
- Require an active session.
- Protect individual UI sections.
- Use ASP.NET Core authorization alongside UltimateAuth.
- Configure authorization with built-in and custom routers.
- Understand the role of `UAuthApp` and `UAuthComponentBase`.

> **Security:** Blazor authorization controls which UI components are rendered. Protected API endpoints, data, and server operations must enforce authorization independently on the server. Client-side authorization must never be the only security boundary.

## Prerequisites

You should have a working UltimateAuth Blazor application with authentication configured.

If you haven't created one yet, start with the [QuickStart](/docs/getting-started/quickstart).

The examples in this guide use:

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Client.Blazor
@using CodeBeam.UltimateAuth.Core.Contracts
@using Microsoft.AspNetCore.Authorization
```

You can add these namespaces to your application's `_Imports.razor` file to make them available across components.

## 1. Protect a Page with UAuthAuthorize

The simplest way to protect a routed Blazor page is to add the `[UAuthAuthorize]` attribute.

Create `Pages/Dashboard.razor`:

```razor
@page "/dashboard"
@attribute [UAuthAuthorize]

<h1>Dashboard</h1>

<p>You are authenticated.</p>
```

That's it.

When a user navigates to `/dashboard`, UltimateAuth evaluates the page's authorization requirements before rendering the page component.

- **Authenticated user with an acceptable session state:** The page can be rendered.
- **Anonymous user:** Authorization is denied.
- **Inactive session:** Authorization is denied when an inactive session state is available and `RequireActive` is enabled.

By default, `[UAuthAuthorize]` requires an authenticated user and an active session where session-state information is present.

No explicit role or permission is required.

### Does the page need to inherit from UAuthComponentBase?

**No.**

The page above works without inheriting from any UltimateAuth base class.

Authorization is enforced by `UAuthAuthorizeRouteView`, not by the page's base class.

This also means an unauthorized routed page is not instantiated by UltimateAuth's route view.

## 2. Require a Role

Use the `Roles` property to restrict access to users with a specific role.

```razor
@page "/admin"
@attribute [UAuthAuthorize(Roles = "Admin")]

<h1>Administration</h1>

<p>Only administrators can access this page.</p>
```

The user must be authenticated and have the `Admin` role.

You can also declare multiple roles:

```razor
@page "/management"
@attribute [UAuthAuthorize(Roles = "Admin,Manager")]

<h1>Management</h1>
```

With the default `Category` match mode, having **either `Admin` or `Manager`** is sufficient, provided the other configured authorization categories also succeed.

Role names must correspond to the roles recognized by UltimateAuth's authentication state.

## 3. Require a Permission

For more granular authorization, use `Permissions`.

```razor
@page "/users"
@attribute [UAuthAuthorize(Permissions = "users.read.self")]

<h1>Users</h1>

<p>You have permission to view users.</p>
```

UltimateAuth checks the requested permission against the current `UAuthState`.

You can declare multiple permissions:

```razor
@page "/reports"
@attribute [UAuthAuthorize(Permissions = "reports.read.self,reports.manage.admin")]
    
<h1>Reports</h1>
```

By default, the user needs at least one of these permissions.

Permission strings must match the permissions represented in the authenticated user's UltimateAuth state.

> **Tip:** Prefer permissions for operations such as reading reports, managing users, or editing records. Roles are useful for broader organizational responsibilities, while permissions can express specific capabilities.

## 4. Combine Roles and Permissions

You can specify both `Roles` and `Permissions`.

```razor
@page "/administration/users"
@attribute [UAuthAuthorize(Roles = "Admin", Permissions = "users.manage.admin")]

<h1>User Management</h1>
```

With the default `MatchMode = Category`, the user must satisfy:

**Admin role AND users.manage permission**

This is because `Category` requires at least one match from each configured category.

If multiple roles and permissions are provided:

```razor
@page "/management/users"
@attribute [UAuthAuthorize(Roles = "Admin,Manager", Permissions = "users.read.self,users.manage.admin")]
```

The requirement becomes:

**(Admin OR Manager) AND (users.read.self OR users.manage.admin)**

This is the default authorization behavior.

## 5. Understand Match Modes

UltimateAuth supports three authorization matching modes:

| Match Mode | Behavior |
|---|---|
| `Category` | At least one matching value in each configured category |
| `Any` | At least one configured role or permission must match |
| `All` | Every configured role and permission must match |

The default is `AuthorizationMatchMode.Category`.

### Category — the default

```razor
@page "/category-example"
@attribute [UAuthAuthorize(Roles = "Admin,Manager", Permissions = "reports.read,reports.write", MatchMode = AuthorizationMatchMode.Category)]
```

The user must have:

- `Admin` **or** `Manager`.
- `reports.read` **or** `reports.write`.

Both configured categories must succeed.

Use `Category` when roles and permissions represent independent requirements but alternatives within each category are acceptable.

### Any — one match is enough

```razor
@page "/any-example"
@attribute [UAuthAuthorize(Roles = "Admin,Auditor", Permissions = "reports.read.admin", MatchMode = AuthorizationMatchMode.Any)]
```

The user needs at least one of:

- `Admin` role.
- `Auditor` role.
- `reports.read.admin` permission.

This is useful for pages with several alternative authorization paths.

Be careful when combining high-privilege roles and lower-privilege permissions with `Any`: a single matching permission grants access.

### All — every value is required

```razor
@page "/all-example"
@attribute [UAuthAuthorize(Roles = "Admin,SecurityOfficer", Permissions = "audit.read.self,audit.export.admin", MatchMode = AuthorizationMatchMode.All)]
```

The user must have:

- `Admin` role.
- `SecurityOfficer` role.
- `audit.read.self` permission.
- `audit.export.admin` permission.

Use `All` when every declared requirement is mandatory.

### Choosing a match mode

| Requirement | Recommended mode |
|---|---|
| Any authenticated user | Parameterless attribute |
| One required role | Default `Category` |
| Any of several roles | Default `Category` |
| One role AND one permission | Default `Category` |
| Role OR permission | `Any` |
| All listed roles AND permissions | `All` |

If roles and permissions are omitted entirely, an authenticated user satisfies the authorization requirement regardless of match mode, subject to the session-state requirement.

## 6. Require an Active Session

By default, `[UAuthAuthorize]` uses:

```csharp
RequireActive = true
```

The default configuration can be written explicitly:

```razor
@page "/account/settings"
@attribute [UAuthAuthorize(RequireActive = true)]

<h1>Account Settings</h1>
```

When UltimateAuth has session-state information and the session is not active, authorization returns an inactive result.

If your application needs to allow an authenticated user regardless of a reported non-active session state, you can disable this requirement:

```razor
@page "/session-information"
@attribute [UAuthAuthorize(RequireActive = false)]

<h1>Session Information</h1>
```

This changes only the session-state condition. Authentication is still required.

> **Important:** `RequireActive` evaluates the session state currently available to the Blazor client. It does not independently validate a session against the server for every render. Server-side session validation and revocation enforcement remain necessary.

When no session-state value is available, the default evaluator does not reject the user solely because that value is absent.

## 7. Protect Part of a Page with UAuthStateView

Sometimes you want the page to remain publicly accessible while protecting only a specific section.

Use `UAuthStateView` for conditional rendering.

```razor
@page "/products"

<h1>Products</h1>

<p>Everyone can view this page.</p>

<UAuthStateView Roles="Admin">
    <Authorized>
        <button>Edit Products</button>
    </Authorized>
    <NotAuthorized>
        <p>Administrative tools are unavailable.</p>
    </NotAuthorized>
</UAuthStateView>
```

The page itself remains accessible to everyone.

However, the administrative section is only rendered for an authorized user.

`UAuthStateView` uses the same `IUAuthAuthorizationEvaluator` as `UAuthAuthorizeRouteView`, so matching roles, permissions, and match modes have consistent semantics.

### Basic authentication-only section

```razor
<UAuthStateView>
    <Authorized>
        <p>Welcome back!</p>
    </Authorized>
    <NotAuthorized>
        <p>Sign in to access your account features.</p>
    </NotAuthorized>
</UAuthStateView>
```

As with parameterless `[UAuthAuthorize]`, authentication is required, along with an acceptable session state when `RequireActive` is true.

### Protect a section with permissions

```razor
<UAuthStateView Permissions="orders.refund.self">
    <Authorized>
        <button>Refund Order</button>
    </Authorized>
    <NotAuthorized>
        <p>You cannot refund orders.</p>
    </NotAuthorized>
</UAuthStateView>
```

### Combine requirements in a section

```razor
<UAuthStateView Roles="Admin,Manager" Permissions="orders.read.self,orders.manage.admin" MatchMode="AuthorizationMatchMode.Category">
    <Authorized>
        <p>Order management tools are available.</p>
    </Authorized>
    <NotAuthorized>
        <p>Access denied.</p>
    </NotAuthorized>
</UAuthStateView>
```

This uses the same `Category` semantics as the routed page attribute.

### Access the current authentication state

The `Authorized` fragment receives the current `UAuthState` as its context.

```razor
<UAuthStateView>
    <Authorized Context="state">
        <p>Hello, @state.Identity?.DisplayName!</p>
    </Authorized>
    <NotAuthorized>
        <p>Please sign in.</p>
    </NotAuthorized>
</UAuthStateView>
```

This allows authorized content to access identity information without requiring a custom base class.

## 8. Handle Unauthorized, Inactive, and Authorizing States

`UAuthStateView` supports multiple rendering states.

```razor
<UAuthStateView Roles="Admin"
                RequireActive="true">
    <Authorized>
        <p>Administrator access granted.</p>
    </Authorized>

    <NotAuthorized>
        <p>You do not have permission to access this section.</p>
    </NotAuthorized>

    <Inactive Context="state">
        <p>Your session is not active.</p>
    </Inactive>

    <Authorizing>
        <p>Checking your permissions...</p>
    </Authorizing>
</UAuthStateView>
```

| Fragment | When displayed |
|---|---|
| `Authorized` | Authorization succeeds |
| `NotAuthorized` | Authorization is denied |
| `Inactive` | A non-active session is detected and `RequireActive` is true |
| `Authorizing` | Authorization evaluation is in progress |

If `Inactive` is not supplied, the component falls back to the `NotAuthorized` fragment.

If `Authorizing` is not supplied, unauthorized content is used while a new authorization result is pending.

During reevaluation, previously authorized section content is removed before an asynchronous policy evaluation completes. This prevents stale authorized content from remaining visible during that pending evaluation.

## 9. Use an ASP.NET Core Authorization Policy

`UAuthStateView` supports the `Policy` parameter.

First, register a policy using ASP.NET Core authorization:

```csharp
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("CanViewReports", policy => policy.RequireClaim("reports_access", "granted"));
});
```

Then use that policy in a UI section:

```razor
<UAuthStateView Policy="CanViewReports">
    <Authorized>
        <p>Reports are available.</p>
    </Authorized>
    <NotAuthorized>
        <p>Reports access is restricted.</p>
    </NotAuthorized>
</UAuthStateView>
```

The policy is evaluated through ASP.NET Core's `IAuthorizationService`, using the claims principal produced from the current UltimateAuth state.

You can also combine a policy with roles and permissions:

```razor
<UAuthStateView Roles="Admin" Permissions="reports.read" Policy="CanViewReports" MatchMode="AuthorizationMatchMode.All">
    <Authorized>
        <p>All requirements passed.</p>
    </Authorized>
</UAuthStateView>
```

For `All`, every configured requirement must succeed.

For `Any`, the policy is another possible successful condition.

For `Category`, the policy must succeed alongside the configured role and permission categories.

> **Current API distinction:** `[UAuthAuthorize]` currently exposes `Roles`, `Permissions`, `MatchMode`, and `RequireActive`, but not `Policy`. Use `UAuthStateView` for policy-based section rendering, or ASP.NET Core `[Authorize(Policy = "...")]` for policy-protected routed pages.

## 10. Protect a Page with ASP.NET Core Authorize

UltimateAuth integrates with ASP.NET Core authorization.

You can use the standard `[Authorize]` attribute on routed Blazor pages.

```razor
@page "/account"
@attribute [Authorize]

<h1>Account</h1>

<p>This page requires ASP.NET Core authorization.</p>
```

Or require a role:

```razor
@page "/administration"
@attribute [Authorize(Roles = "Admin")]

<h1>Administration</h1>
```

Or a registered policy:

```razor
@page "/reports"
@attribute [Authorize(Policy = "CanViewReports")]

<h1>Reports</h1>
```

These checks are performed through ASP.NET Core's `AuthorizeRouteView` and its authentication and authorization infrastructure.

UltimateAuth provides a Blazor `AuthenticationStateProvider` that integrates UltimateAuth authentication state with ASP.NET Core.

### Which attribute should I use?

| Feature | `[UAuthAuthorize]` | `[Authorize]` |
|---|---|---|
| Requires authentication | Yes | Yes |
| Roles | Yes | Yes |
| UltimateAuth permissions | Yes | Via separately configured ASP.NET Core policies or handlers |
| `AuthorizationMatchMode` | Yes | No direct equivalent property |
| `RequireActive` | Yes | No direct equivalent property |
| Named ASP.NET Core policy | Not currently | Yes |
| Routed Blazor page | Yes | Yes |

Use `[UAuthAuthorize]` when working directly with UltimateAuth roles, permissions, and session-aware requirements.

Use `[Authorize]` when relying on ASP.NET Core authorization policies, requirements, or existing authorization infrastructure.

### Combine both attributes

You can declare both on the same routed page:

```razor
@page "/secure-reports"

@attribute [UAuthAuthorize(Permissions = "reports.read")]
@attribute [Authorize(Roles = "Admin")]

<h1>Secure Reports</h1>
```

The page is rendered only when **both authorization systems allow access**.

UltimateAuth evaluates `[UAuthAuthorize]`, and the underlying ASP.NET Core `AuthorizeRouteView` evaluates `[Authorize]`.

The requirements are effectively combined using AND semantics across the two mechanisms.

> **Note:** ASP.NET Core `[Authorize]` uses the configured ASP.NET Core authentication state. `[UAuthAuthorize]` uses `UAuthState`. UltimateAuth integrates these models, but custom authentication providers must keep their states consistent.

## 11. How UAuthApp Protects Routes

`UAuthApp` is the root Blazor integration component for UltimateAuth.

It provides:

- Client runtime initialization.
- The current `UAuthState` as a cascading parameter.
- ASP.NET Core cascading authentication state.
- Session lifecycle coordination.
- Runtime initialization readiness.
- Optional built-in routing.

### Built-in router

If you use:

```razor
<UAuthApp UseBuiltInRouter="true" AppAssembly="typeof(Program).Assembly" DefaultLayout="typeof(Layout.MainLayout)" />
```

UltimateAuth automatically configures its router to use `UAuthAuthorizeRouteView`.

You do not need to add a separate `AuthorizeRouteView` or `UAuthAuthorizeRouteView`.

The built-in router supports both `[UAuthAuthorize]` and ASP.NET Core `[Authorize]`.

It also supports an application-level `NotAuthorized` fragment:

```razor
<UAuthApp UseBuiltInRouter="true"
          AppAssembly="typeof(Program).Assembly"
          DefaultLayout="typeof(Layout.MainLayout)">
    <NotAuthorized>
        <h1>Access Denied</h1>
        <p>You are not authorized to access this page.</p>
    </NotAuthorized>
</UAuthApp>
```

This fragment is displayed when route authorization fails.

### Runtime initialization

`UAuthApp` initializes the client authentication state before allowing UltimateAuth-protected routes to be rendered.

This is important because authentication state may not be immediately available when a Blazor application first starts, particularly when restoring a session.

During initialization or asynchronous authorization evaluation, `UAuthAuthorizeRouteView` can display an `Authorizing` fragment.

If one is not supplied, the protected route content remains unrendered while authorization is pending.

### Manual and Reactive render modes

`UAuthApp` supports `UAuthRenderMode.Manual` and `UAuthRenderMode.Reactive`.

These control how UltimateAuth state changes affect general component rendering.

Route authorization itself subscribes to relevant authentication state changes, and `UAuthStateView` also reacts to such changes.

You do not need to enable reactive rendering merely to make `[UAuthAuthorize]` work.

## 12. Protect Pages with a Custom Router

If your application uses its own Blazor `Router`, set `UseBuiltInRouter="false"` on `UAuthApp`.

Then use `UAuthAuthorizeRouteView` inside the router's `Found` fragment.

```razor
<UAuthApp UseBuiltInRouter="false">
    <Router AppAssembly="typeof(Program).Assembly">
        <Found Context="routeData">
            <UAuthAuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">

                <NotAuthorized>
                    <h1>Access Denied</h1>
                    <p>You cannot access this page.</p>
                </NotAuthorized>

                <Authorizing>
                    <p>Checking access...</p>
                </Authorizing>

            </UAuthAuthorizeRouteView>

            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </Found>
    </Router>
</UAuthApp>
```

`UAuthAuthorizeRouteView` wraps ASP.NET Core's `AuthorizeRouteView` internally.

This means it supports both authorization systems, while checking UltimateAuth-specific requirements before the protected page component is instantiated.

**Do not replace it with a plain `RouteView` or `AuthorizeRouteView` when your routed pages depend on `[UAuthAuthorize]`.**

A plain ASP.NET Core `AuthorizeRouteView` does not evaluate the UltimateAuth-specific attribute by itself.

If your custom router uses additional route assemblies, pass the appropriate `AdditionalAssemblies` to the `Router` as needed.

## 13. Do I Need UAuthComponentBase?

**Not for authorization.**

Consider this page:

```razor
@page "/settings"
@attribute [UAuthAuthorize]

<h1>Settings</h1>
```

It is protected when routed through `UAuthAuthorizeRouteView`, even though it doesn't inherit from `UAuthComponentBase`.

You can still use `UAuthComponentBase` when you need access to the UltimateAuth state lifecycle.

```razor
@page "/profile"
@attribute [UAuthAuthorize]
@inherits UAuthComponentBase

<h1>Profile</h1>

<p>User: @AuthState.Identity?.DisplayName</p>

@code {
    protected override Task HandleAuthStateChangedAsync(
        UAuthStateChangeReason reason)
    {
        // React to authentication state changes.
        return Task.CompletedTask;
    }
}
```

`UAuthComponentBase` provides:

- Access to cascading `UAuthState`.
- Authentication state change notifications.
- Automatic rerendering when the state changes, by default.
- An asynchronous lifecycle hook.
- Event subscription cleanup on disposal.

However, it no longer performs page authorization itself.

### Can I use UAuthAuthorize on a regular child component?

The attribute only declares metadata. It does not enforce authorization on its own.

For example:

```razor
@attribute [UAuthAuthorize(Roles = "Admin")]

<div>Administrative tools</div>
```

If this is an ordinary child component rendered directly inside another page, `[UAuthAuthorize]` is not automatically evaluated by `UAuthAuthorizeRouteView`.

Route authorization is performed for routed page components.

For conditional content inside a page, use `UAuthStateView` instead.

## 14. Advanced: Customize Authorization Evaluation

UltimateAuth uses the `IUAuthAuthorizationEvaluator` service for Blazor authorization.

The default evaluator is registered by:

```csharp
builder.Services.AddUltimateAuthClientBlazor();
```

It evaluates:

- Authentication status.
- Session state when required.
- Roles.
- Permissions.
- ASP.NET Core policies supplied through authorization requirements.
- The configured match mode.

The evaluator is registered with `TryAddScoped`, allowing applications to provide their own implementation through dependency injection.

If you need custom application-specific authorization behavior, you can implement `IUAuthAuthorizationEvaluator`.

A replacement evaluator receives the current `UAuthState` and a `UAuthAuthorizationRequirement` and returns a `UAuthAuthorizationResult`.

A custom implementation should preserve secure defaults, especially for anonymous users, inactive sessions, and failed authorization checks.

Keep authorization evaluation separate from component rendering and navigation logic.

## 15. Important Security Considerations

### Blazor UI authorization is not API authorization

An unauthorized user should not see or instantiate a protected routed page. However, hiding UI does not secure the HTTP endpoint or underlying data source.

For example:

```razor
@page "/users"
@attribute [UAuthAuthorize(Roles = "Admin")]
```

This protects the Blazor page through UltimateAuth's router.

It does **not** automatically protect an unrelated API endpoint such as:

```text
GET /api/users
```

That endpoint must independently validate the caller and enforce the required authorization.

For Blazor Server, the same principle applies to sensitive server-side operations, even when they are invoked without an HTTP call from the component.

### UAuthAuthorize is currently a Blazor attribute

The current `[UAuthAuthorize]` attribute is interpreted by UltimateAuth's Blazor authorization pipeline.

It does not, by itself, secure ASP.NET Core controllers or Minimal API endpoints.

Use the server-side authorization mechanisms appropriate for those endpoints.

### Revocation and role changes

`UAuthAuthorizeRouteView` and `UAuthStateView` respond to authentication state changes.

When the available `UAuthState` changes, authorization can be reevaluated.

However, a remote role change or server-side session revocation is not necessarily observed immediately by the Blazor client.

Server-side authorization must remain authoritative, even while a client retains stale state.

### Authorization during navigation

A protected routed page is not instantiated until UltimateAuth authorization succeeds.

When authorization becomes invalid, the route view stops rendering the protected page.

This is different from implementing a redirect inside the page's `OnInitialized` or `OnAfterRender` method, where the component may already have been instantiated.

### Do not rely on hidden controls for security

`UAuthStateView` is useful for showing or hiding buttons, navigation entries, and sections.

Always authorize the underlying operation independently.

For example, hiding a `Delete User` button does not remove the need to authorize the actual delete request.

## 16. Troubleshooting

### UAuthAuthorize does not prevent access

First, make sure you're using **UltimateAuth v0.1.1 or later**.

Earlier versions relied on `UAuthComponentBase` for authorization checks. Starting with v0.1.1, `[UAuthAuthorize]` is enforced at the routing level through `UAuthAuthorizeRouteView`.

If authorization is not working as expected:

1. Verify that `CodeBeam.UltimateAuth.Client.Blazor` is version `0.1.1` or later.
2. If you're using the built-in router, make sure `UseBuiltInRouter="true"` is configured on `UAuthApp`.
3. If you're using a custom router, replace `RouteView` or `AuthorizeRouteView` with `UAuthAuthorizeRouteView`.
4. Make sure the page is routed within the `UAuthApp` context.
5. Confirm that the user meets the authentication, role, permission, and session-state requirements.

**Upgrading from v0.1.0?** See [Migrating to v0.1.1](/docs/migration/v0.1.1) for the required changes.

### The protected page appears blank during startup

UltimateAuth may still be initializing or evaluating authorization.

With a custom router, provide an `Authorizing` fragment for a loading indicator.

### UAuthStateView reports a missing cascading UAuthState

Make sure the component is rendered inside `UAuthApp`, or another host that correctly provides the required cascading UltimateAuth state.

### ASP.NET Core authorization reports a missing AuthenticationState

Make sure ASP.NET Core's authentication state infrastructure is available.

`UAuthApp` provides a `CascadingAuthenticationState` within its child content.

If you create a custom host outside `UAuthApp`, you must provide the appropriate ASP.NET Core cascading authentication state yourself.

### Roles or permissions are unexpectedly denied

Confirm that:

- The user is authenticated.
- The state contains the expected role or permission.
- The correct match mode is selected.
- The user's session state satisfies `RequireActive`.
- The client state has been updated after any server-side changes.

### I need a policy directly on a routed page

Use ASP.NET Core's `[Authorize(Policy = "...")]` attribute.

The current `[UAuthAuthorize]` attribute does not expose a `Policy` property.

## Summary

| Scenario | Recommended approach |
|---|---|
| Protect an entire Blazor page | `[UAuthAuthorize]` |
| Require an UltimateAuth role | `[UAuthAuthorize(Roles = "...")]` |
| Require an UltimateAuth permission | `[UAuthAuthorize(Permissions = "...")]` |
| Combine roles and permissions | `MatchMode` |
| Require ASP.NET Core policy authorization | `[Authorize(Policy = "...")]` |
| Combine ASP.NET Core and UltimateAuth | Use both attributes |
| Protect an individual UI section | `UAuthStateView` |
| Use built-in routing | `UAuthApp UseBuiltInRouter="true"` |
| Use custom routing | `UAuthAuthorizeRouteView` |
| React to authentication lifecycle changes | `UAuthComponentBase` |
| Protect an API or server operation | Enforce authorization on the server |

UltimateAuth's Blazor authorization model allows you to declare requirements at the page level, conditionally render protected sections, and integrate with ASP.NET Core authorization without coupling page protection to a specific base class.

## Next Steps

- [QuickStart](/docs/getting-started/quickstart) — Create a working UltimateAuth application.
- [Authentication Model](/docs/fundamentals/auth-model) — Understand UltimateAuth authentication concepts.
- [Migration to v0.1.1](/docs/migration/v0.1.1) — Update custom routers and existing Blazor authorization code.
