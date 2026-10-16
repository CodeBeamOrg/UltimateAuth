---
title: Authentication
order: 2
group: client
---

# Authentication

The UltimateAuth Client SDK provides a high-level API for authentication workflows, including login, PKCE, session validation, refresh, logout, and device-specific session revocation.

These operations are exposed through `UAuthClient.Flows`, which implements the `IFlowClient` contract.

This guide shows how to use the authentication client in application code, handle structured results, configure interactive behavior, and choose the correct flow for different client environments.

## 1. Accessing the Authentication Client

In a Blazor component, inject `IUAuthClient`:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient
```

Authentication operations are available through the `Flows` property.

```csharp
await UAuthClient.Flows.RefreshAsync();
await UAuthClient.Flows.ValidateAsync();
await UAuthClient.Flows.LogoutAsync();
```

The relevant methods are defined by `IFlowClient`.

| Method | Purpose |
|---|---|
| `LoginAsync` | Submit a direct login flow |
| `TryLoginAsync` | Attempt login with configurable try/commit behavior |
| `BeginPkceAsync` | Start a PKCE authentication flow |
| `CompletePkceLoginAsync` | Complete a PKCE login |
| `TryCompletePkceLoginAsync` | Complete PKCE with structured try/commit behavior |
| `RefreshAsync` | Refresh or continue authentication |
| `ValidateAsync` | Validate current authentication state |
| `LogoutAsync` | Log out the current session |
| `LogoutMyDeviceAsync` | Revoke a selected device-associated chain |
| `LogoutMyOtherDevicesAsync` | Revoke other devices while retaining the current one |
| `LogoutAllMyDevicesAsync` | Revoke all devices for the current user |

Administrative device logout operations are also available and covered later in this guide.

## 2. Choosing the Correct Login Flow

Before implementing login, determine whether your application is a trusted server-hosted client or a public client.

| Client environment | Recommended flow |
|---|---|
| Blazor Server | Direct login or supported interactive login flow |
| UAuthHub | Server-hosted authentication flow |
| Blazor WebAssembly | PKCE |
| Browser SPA | PKCE |
| Public native application | PKCE |

Public clients must not rely on embedding a secret or trusting browser-executed code to protect credentials.

UltimateAuth restricts direct credential posting based on the configured client profile and login options.

For public clients, use the PKCE flow instead of enabling direct credential posting.

## 3. Direct Login

`LoginAsync` performs a direct login submission.

```csharp
var request = new LoginRequest
{
    Identifier = "user@example.com",
    Secret = "password"
};

await UAuthClient.Flows.LoginAsync(request);
```

The method returns `Task`, not a structured login result.

It submits the login payload through the configured navigation-based transport.

Depending on the hosting environment, this may initiate navigation or redirect behavior.

### LoginRequest Properties

`LoginRequest` supports the following properties:

| Property | Purpose |
|---|---|
| `Identifier` | Login identifier |
| `Secret` | Credential secret |
| `CredentialType` | Credential type; defaults to `Password` |
| `Metadata` | Optional request metadata |
| `RequestTokens` | Hint to request tokens when supported |
| `PreviewReceipt` | Optional receipt associated with a prior login preview |

Example:

```csharp
var request = new LoginRequest
{
    Identifier = email,
    Secret = password,
    CredentialType = CredentialType.Password,
    RequestTokens = true
};

await UAuthClient.Flows.LoginAsync(request);
```

`RequestTokens` is a hint, not a guarantee that tokens will be issued. The server's authentication mode and policy remain authoritative.

### Redirect After Login

Provide an explicit return URL when necessary:

```csharp
await UAuthClient.Flows.LoginAsync(request, returnUrl: "/dashboard");
```

The direct login flow resolves its return URL in the following order:

1. The explicit `returnUrl` argument.
2. `Login.ReturnUrl` option.
3. `DefaultReturnUrl` option.

For example, client options can specify a default:

```csharp
options.Login.ReturnUrl = "/dashboard";
```

The return URL is part of the login workflow. It should not be treated as an authorization grant.

Applications and servers must still enforce their redirect validation rules.

## 4. Interactive Login with TryLoginAsync

For interactive forms, `TryLoginAsync` provides structured feedback before or during the authentication commit process.

```csharp
var result = await UAuthClient.Flows.TryLoginAsync(
    new LoginRequest
    {
        Identifier = email,
        Secret = password
    },
    UAuthSubmitMode.TryAndCommit);
```

This is generally the more useful API for a login form that needs to display validation feedback.

### Submit Modes

`UAuthSubmitMode` defines three submission behaviors.

| Mode | Behavior |
|---|---|
| `TryOnly` | Execute the try operation and return its result without committing authentication |
| `DirectCommit` | Submit directly to the commit endpoint without a separate try operation |
| `TryAndCommit` | Try first, then commit when the try operation succeeds |

These modes control client workflow, not whether the server validates authentication.

**DirectCommit does not bypass credential verification or server security requirements.**

### TryOnly: Validate Without Login

Use `TryOnly` when you want structured feedback without establishing an authenticated session.

```csharp
var result = await UAuthClient.Flows.TryLoginAsync(
    new LoginRequest
    {
        Identifier = email,
        Secret = password
    },
    UAuthSubmitMode.TryOnly);

if (result.IsSuccess)
{
    // The try operation succeeded.
    // Authentication has not been committed.
}
else
{
    // Display the appropriate feedback.
}
```

This can be useful for a custom interactive workflow.

A successful try result must not be interpreted as an authenticated application session.

### TryAndCommit: Interactive Login

Use `TryAndCommit` to combine structured validation with authentication commit.

```csharp
var result = await UAuthClient.Flows.TryLoginAsync(
    new LoginRequest
    {
        Identifier = email,
        Secret = password
    },
    UAuthSubmitMode.TryAndCommit,
    returnUrl: "/dashboard");

if (!result.IsSuccess)
{
    // Show validation feedback.
}
```

When the try stage succeeds, the client proceeds to the commit operation through its configured transport.

This can involve navigation, so application code should not assume that execution always continues within the same component instance.

### DirectCommit: Single Commit Submission

```csharp
await UAuthClient.Flows.TryLoginAsync(
    new LoginRequest
    {
        Identifier = email,
        Secret = password
    },
    UAuthSubmitMode.DirectCommit);
```

In this mode, the SDK submits the commit operation directly.

The method returns a `TryLoginResult` with `IsSuccess = true` after the navigation submission completes, but this is not a structured server-side authentication success confirmation.

If you need structured credential validation feedback, prefer `TryAndCommit`.

## 5. Handling Login Results

`TryLoginAsync` returns `TryLoginResult`.

Its available properties include:

| Property | Meaning |
|---|---|
| `IsSuccess` | Whether the try operation succeeded |
| `Reason` | Optional authentication failure reason |
| `RemainingAttempts` | Remaining attempts, when provided |
| `LockoutUntilUtc` | Lockout expiration, when provided |
| `RequiresMfa` | Whether additional authentication is required |
| `PreviewReceipt` | Optional receipt for subsequent flow handling |

Example:

```csharp
var result = await UAuthClient.Flows.TryLoginAsync(request, UAuthSubmitMode.TryOnly);

if (!result.IsSuccess)
{
    if (result.LockoutUntilUtc is not null)
    {
        errorMessage = $"Login temporarily unavailable until {result.LockoutUntilUtc:u}";
    }
    else
    {
        errorMessage = "Unable to sign in.";
    }

    return;
}

if (result.RequiresMfa)
{
    // Continue with the supported additional authentication flow.
}
```

Do not expose sensitive internal authentication details unnecessarily.

For public-facing login forms, generic failure messages may be preferable to displaying every failure reason.

### Handling Transport and Protocol Errors

A structured authentication failure is different from a network or protocol failure.

For example:

```csharp
try
{
    var result = await UAuthClient.Flows.TryLoginAsync(request, UAuthSubmitMode.TryAndCommit);

    if (!result.IsSuccess)
    {
        errorMessage = "Unable to sign in.";
    }
}
catch (UAuthTransportException)
{
    errorMessage = "Authentication service is unavailable.";
}
catch (UAuthProtocolException)
{
    errorMessage = "An authentication protocol error occurred.";
}
```

Use the corresponding UltimateAuth client error namespace when referencing these exception types.

A failed authentication attempt should normally be handled through the structured try result where supported. Transport and protocol exceptions indicate different failure categories.

## 6. PKCE Authentication

PKCE provides a proof mechanism for authorization-code-based authentication flows.

UltimateAuth supports PKCE using an S256 code challenge.

The SDK generates a cryptographically random verifier, derives its challenge, and coordinates the authorization flow.

Public clients such as Blazor WebAssembly should use PKCE rather than direct credential posting.

### Start PKCE

```csharp
await UAuthClient.Flows.BeginPkceAsync();
```

You can provide a return URL:

```csharp
await UAuthClient.Flows.BeginPkceAsync(returnUrl: "/dashboard");
```

The client performs the following operations:

1. Generates a PKCE code verifier.
2. Derives an S256 code challenge.
3. Sends the authorization request.
4. Receives an authorization code.
5. Invokes the configured authorization callback, if any.
6. Navigates to the configured UAuthHub login flow when automatic redirection is enabled.

The authorization code and verifier are used later to complete the flow.

### Configure PKCE

The relevant client options include:

```csharp
options.Pkce.Enabled = true;
options.Pkce.AutoRedirect = true;
options.Pkce.ReturnUrl = "/dashboard";
```

PKCE is enabled by default in `UAuthClientPkceLoginFlowOptions`.

Automatic redirection is also enabled by default.

### Observe Authorization

You can register a callback that runs after the authorization code is received and before the automatic Hub redirect.

```csharp
options.Pkce.OnAuthorized = async response =>
{
    // Observe the authorization response.
    await Task.CompletedTask;
};
```

This callback is useful for application-specific coordination.

Do not log authorization codes or expose them to unrelated components.

### Disable Automatic Redirection

```csharp
options.Pkce.AutoRedirect = false;
```

With this configuration, `BeginPkceAsync` still performs the authorization request and invokes `OnAuthorized`, but it does not automatically navigate to the Hub login endpoint.

The application becomes responsible for coordinating the remaining flow.

This is an advanced integration option.

A custom implementation must preserve the authorization code, verifier, and relevant context securely and complete the flow using the supported protocol.

## 7. Complete PKCE Login

`CompletePkceLoginAsync` submits the PKCE completion request.

```csharp
var request = new PkceCompleteRequest
{
    AuthorizationCode = authorizationCode,
    CodeVerifier = codeVerifier,
    Identifier = email,
    Secret = password,
    ReturnUrl = "/dashboard"
};

await UAuthClient.Flows.CompletePkceLoginAsync(request);
```

The request model includes:

| Property | Purpose |
|---|---|
| `AuthorizationCode` | Code obtained from the authorization step |
| `CodeVerifier` | Original PKCE verifier |
| `Identifier` | Login identifier |
| `Secret` | Login credential |
| `ReturnUrl` | Optional post-login destination |
| `HubSessionId` | Optional Hub session context |

The authorization code and verifier must correspond to the same PKCE flow.

Do not generate a new verifier when completing an existing authorization.

### Interactive PKCE Completion

For structured feedback, use `TryCompletePkceLoginAsync`.

```csharp
var result = await UAuthClient.Flows.TryCompletePkceLoginAsync(
    request,
    UAuthSubmitMode.TryAndCommit);

if (!result.IsSuccess)
{
    // Display PKCE login feedback.
}
```

The same submit modes are supported:

- `TryOnly`
- `DirectCommit`
- `TryAndCommit`

### Try PKCE Without Commit

```csharp
var result = await UAuthClient.Flows.TryCompletePkceLoginAsync(request, UAuthSubmitMode.TryOnly);
```

This returns a `TryPkceLoginResult` without committing a successful login.

The result includes:

```csharp
result.IsSuccess;
result.Reason;
result.RemainingAttempts;
result.LockoutUntilUtc;
result.RequiresMfa;
result.RetryWithNewPkce;
```

### RetryWithNewPkce

`RetryWithNewPkce` indicates that the client should begin a new PKCE authorization sequence rather than continuing with the existing authorization material.

For example:

```csharp
if (result.RetryWithNewPkce)
{
    await UAuthClient.Flows.BeginPkceAsync("/login");
    return;
}
```

This is useful when the current PKCE context can no longer be used.

The application should not repeatedly resubmit the same invalid authorization code and verifier.

## 8. Direct Credential Posting Restrictions

UltimateAuth restricts direct credential posting according to the configured client profile.

The client option is:

```csharp
options.Login.AllowCredentialPost = false;
```

This is the default.

If the current profile is not permitted to post credentials directly and the option has not enabled that capability, direct login methods throw an `InvalidOperationException`.

For example, attempting direct login from a public client may produce an error indicating that PKCE is required.

### Trusted Server-Hosted Clients

The option can be explicitly configured for a trusted server-hosted integration where direct credential posting is appropriate:

```csharp
options.Login.AllowCredentialPost = true;
```

This must not be used to bypass PKCE requirements in a public browser application.

**Do not enable direct credential posting in production Blazor WebAssembly or SPA clients.**

A client profile is an integration descriptor, not cryptographic proof that the caller is trusted.

Server-side security requirements remain authoritative.

## 9. Refresh Authentication

`RefreshAsync` requests continuation of the current authentication state.

```csharp
var result = await UAuthClient.Flows.RefreshAsync();
```

The method returns `RefreshResult`.

```csharp
public sealed record RefreshResult
{
    public bool IsSuccess { get; init; }
    public int Status { get; init; }
    public RefreshOutcome Outcome { get; init; }
}
```

Refresh behavior depends on the authentication mode.

A refresh operation may update session activity, rotate authentication material, or determine that reauthentication is required.

### Handling Refresh Results

```csharp
var result = await UAuthClient.Flows.RefreshAsync();

if (result.IsSuccess)
{
    // Authentication continuation succeeded.
}
else if (result.Outcome == RefreshOutcome.ReauthRequired)
{
    // The current authentication can no longer continue.
    // Direct the user to the appropriate login flow.
}
else
{
    // Handle another unsuccessful refresh result.
}
```

A `401 Unauthorized` refresh response is handled as a normal reauthentication outcome.

It returns:

```csharp
new RefreshResult
{
    IsSuccess = false,
    Status = 401,
    Outcome = RefreshOutcome.ReauthRequired
};
```

Network and unexpected protocol failures can still produce exceptions.

### Refresh Outcomes

`RefreshOutcome` defines the following values:

| Outcome | Meaning |
|---|---|
| `None` | No specific semantic outcome |
| `Success` | Generic successful refresh |
| `NoOp` | No refresh state change was necessary |
| `Touched` | Applicable session activity was updated |
| `Rotated` | Rotation occurred |
| `ReauthRequired` | Authentication continuation is no longer permitted |

For example:

```csharp
var result = await UAuthClient.Flows.RefreshAsync();

if (!result.IsSuccess)
{
    return;
}

switch (result.Outcome)
{
    case RefreshOutcome.Rotated:
        // Authentication material was rotated.
        break;

    case RefreshOutcome.Touched:
        // Applicable session activity was updated.
        break;

    case RefreshOutcome.NoOp:
        // Successful refresh and no state change was required.
        break;

    case RefreshOutcome.Success:
        // Refresh succeeded without a more specific outcome.
        break;
}
```

### Detailed Refresh Diagnostics

The server may return a generic success outcome rather than exposing the internal refresh action.

Where supported, detailed refresh outcomes can be enabled through server diagnostics:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Diagnostics.EnableRefreshDetails = true;
});
```

This setting is intended for diagnostics and observability.

Applications should not require a specific successful outcome such as `Rotated` or `Touched` to consider refresh successful.

Use `IsSuccess` for the primary success decision.

### Manual and Automatic Refresh

`RefreshAsync` has an optional `isAuto` argument.

```csharp
// Manual refresh
await UAuthClient.Flows.RefreshAsync();

// Refresh initiated by automatic coordination
await UAuthClient.Flows.RefreshAsync(isAuto: true);
```

The parameter distinguishes automatic refresh activity for client diagnostics.

It does not select a different authentication mode or override the server's refresh policy.

## 10. Automatic Refresh Configuration

UltimateAuth exposes client-side automatic refresh options through `AutoRefresh`.

```csharp
options.AutoRefresh.Enabled = true;
options.AutoRefresh.Interval = TimeSpan.FromMinutes(5);
```

`Enabled` controls background refresh coordination.

`Interval` specifies the interval used for background refresh attempts.

The effective defaults and registration behavior depend on the client integration. The options documentation identifies Blazor Server as the environment where background refresh is enabled by default.

### Automatic Refresh Is Not a Security Guarantee

Automatic refresh is a client-side convenience mechanism.

It does not guarantee that a session will remain valid indefinitely.

Authentication may still end because of:

- Expiration.
- Revocation.
- Account security changes.
- Invalid refresh credentials.
- Server-side continuation restrictions.

The client must remain prepared to handle `ReauthRequired`.

## 11. Validate Current Authentication

Use `ValidateAsync` to ask the server to validate the current authentication state.

```csharp
var result = await UAuthClient.Flows.ValidateAsync();

if (result.IsValid)
{
    // Current authentication is valid.
}
else
{
    // Authentication is not currently valid.
}
```

The method returns `AuthValidationResult`.

Its properties include:

| Property | Purpose |
|---|---|
| `State` | Current session state |
| `IsValid` | Whether the state is `Active` |
| `Snapshot` | Optional authentication state snapshot |
| `ChainId` | Optional associated session chain |
| `RootId` | Optional session root |
| `BoundDeviceId` | Optional bound device identifier |

### Inspect Session Context

```csharp
var result = await UAuthClient.Flows.ValidateAsync();

if (result.IsValid && result.ChainId is { } chainId)
{
    // Use chainId for supported session-management operations.
}
```

Not every authentication mode necessarily provides all session hierarchy fields.

Treat optional values as optional.

### ValidateAsync Versus UI Authentication State

`ValidateAsync` performs an explicit server validation operation.

It should not be called on every UI render merely to determine whether a component should display authenticated content.

For UI authentication state, use the supported UltimateAuth state management integration, including `IUAuthStateManager` where appropriate.

Explicit validation is useful when an application needs to check current server-recognized authentication state.

## 12. Logout

`LogoutAsync` performs the current logout flow.

```csharp
await UAuthClient.Flows.LogoutAsync();
```

The method uses the configured logout endpoint and navigation transport.

Depending on the client profile and configuration, logout may involve navigation and authentication state changes.

### Logout Button

A simple Blazor example:

```razor
@using CodeBeam.UltimateAuth.Client
@inject IUAuthClient UAuthClient

<button @onclick="LogoutAsync">Logout</button>

@code {
    private async Task LogoutAsync()
    {
        await UAuthClient.Flows.LogoutAsync();
    }
}
```

Logout is distinct from clearing local UI state.

Removing a local authentication indicator does not necessarily revoke authoritative server-managed authentication state.

Use the supported logout flow to terminate authentication.

## 13. Logout a Specific Device

UltimateAuth supports revoking the session chain associated with a particular device context.

The relevant method is:

```csharp
LogoutMyDeviceAsync(LogoutDeviceRequest request)
```

`LogoutDeviceRequest` requires a `SessionChainId`.

```csharp
var request = new LogoutDeviceRequest
{
    ChainId = chainId
};

var result = await UAuthClient.Flows.LogoutMyDeviceAsync(request);

if (result.IsSuccess)
{
    // The requested device logout operation succeeded.
}
```

This operation targets a session chain, not an arbitrary individual session ID.

### Current Device Behavior

The returned `RevokeResult` can indicate whether the affected chain is the current chain.

When the operation succeeds and `CurrentChain` is true, the client publishes a logout-related state event.

This allows the application to react when the currently authenticated device context has been revoked.

## 14. Logout Other Devices

Use `LogoutMyOtherDevicesAsync` to revoke other device-associated sessions while retaining the current one.

```csharp
var result = await UAuthClient.Flows.LogoutMyOtherDevicesAsync();

if (result.IsSuccess)
{
    // Other devices were logged out.
}
```

This is useful for account security pages that provide a **Log out other devices** action.

The current authentication context is intended to remain active.

The server remains responsible for identifying the current session or chain and enforcing the appropriate revocation scope.

## 15. Logout All Devices

Use `LogoutAllMyDevicesAsync` to revoke all applicable sessions for the current user.

```csharp
var result = await UAuthClient.Flows.LogoutAllMyDevicesAsync();

if (result.IsSuccess)
{
    // All applicable device sessions were revoked.
}
```

On success, the client publishes a logout-related state event.

This operation is useful when a user suspects account compromise or wants to terminate existing authentication sessions across devices.

In JWT-based modes, previously issued access tokens may remain cryptographically valid until expiration unless additional revocation enforcement is configured.

## 16. Administrative Device Logout

UltimateAuth also exposes administrative session revocation operations.

These methods act on a specified user and require appropriate server-side authorization.

### Logout One User Device

```csharp
var result = await UAuthClient.Flows.LogoutUserDeviceAsync(
    userKey,
    new LogoutDeviceRequest
    {
        ChainId = chainId
    });

if (!result.IsSuccess)
{
    // Handle the failure.
}
```

### Logout Other Devices for a User

```csharp
var result = await UAuthClient.Flows.LogoutUserOtherDevicesAsync(userKey, request);
```

Here, `request` is a `LogoutOtherDevicesRequest` identifying the chain to retain.

### Logout All Devices for a User

```csharp
var result = await UAuthClient.Flows.LogoutAllUserDevicesAsync(userKey);
```

Administrative methods do not grant administrative privileges by themselves.

The server must authorize the actor and validate the target user and requested revocation scope.

These operations are particularly relevant to administration panels and account security workflows.

## 17. Building Login Forms with UAuthLoginForm

UltimateAuth provides `UAuthLoginForm`, a reusable Blazor component that handles authentication form submission without requiring developers to implement the underlying flow orchestration themselves.

The component supports password-based login, PKCE completion through UAuthHub, configurable submission modes, structured validation feedback, and customizable UI content.

**You design the login interface. UltimateAuth handles the authentication submission workflow.**

### Basic Usage

A login form can be implemented with the following component:

```razor
@using CodeBeam.UltimateAuth.Client.Blazor

<UAuthLoginForm Identifier="@email" Secret="@password">
    <input @bind="email" placeholder="Email" />
    <input @bind="password" type="password" placeholder="Password" />
    <button type="submit">Sign in</button>
</UAuthLoginForm>

@code {
    private string email = "";
    private string password = "";
}
```

The component automatically uses `TryAndCommit` as its default submission mode.

When submitted, it:

1. Validates that the identifier and secret are present.
2. Creates the appropriate authentication request.
3. Calls the UltimateAuth client flow.
4. Performs the configured try/commit behavior.
5. Exposes structured results through `OnTryResult`.

The example assumes that UltimateAuth's Blazor services have already been registered and that the hosting environment permits the selected login flow.

### Customizing the Login UI

`UAuthLoginForm` uses `ChildContent`, allowing developers to use standard HTML elements or their preferred Blazor component library.

For example, with MudBlazor:

```razor
@using CodeBeam.UltimateAuth.Client.Blazor
@using CodeBeam.UltimateAuth.Client

<UAuthLoginForm Identifier="@email" Secret="@password" SubmitMode="UAuthSubmitMode.TryAndCommit" OnTryResult="HandleLoginResult">
    <MudStack Spacing="3">
        <MudText Typo="Typo.h5">Sign in</MudText>
        <MudTextField @bind-Value="email" Label="Email" Variant="Variant.Outlined" />
        <MudTextField @bind-Value="password" Label="Password" InputType="InputType.Password" Variant="Variant.Outlined" />

        <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled" Color="Color.Primary" FullWidth>Sign in</MudButton>

        @if (!string.IsNullOrWhiteSpace(error))
        {
            <MudAlert Severity="Severity.Error">@error</MudAlert>
        }
    </MudStack>
</UAuthLoginForm>

@code {
    private string email = "";
    private string password = "";
    private string? error;

    private Task HandleLoginResult(IUAuthTryResult result)
    {
        error = result.IsSuccess ? null : "Unable to sign in.";
        return Task.CompletedTask;
    }
}
```

The form controls remain under your control.

UltimateAuth does not require a particular visual design, CSS framework, or component library.

Because the component renders an HTML form internally, place the submit button inside its child content and avoid nesting another HTML form within it.

### Submission Modes

Use `SubmitMode` to control how the authentication attempt is executed.

```razor
<UAuthLoginForm Identifier="@email" Secret="@password" SubmitMode="UAuthSubmitMode.TryAndCommit">
    ...
</UAuthLoginForm>
```

| Mode | Behavior |
|---|---|
| `TryOnly` | Validates the authentication attempt without committing login |
| `TryAndCommit` | Attempts authentication and commits on success |
| `DirectCommit` | Submits directly to the commit flow |

`TryAndCommit` is the default.

For interactive login forms that need validation feedback, it is generally the recommended choice.

`DirectCommit` does not invoke `OnTryResult`, because it does not perform the structured try operation.

### Handling Authentication Results

The component exposes an `OnTryResult` callback:

```razor
<UAuthLoginForm Identifier="@email" Secret="@password" OnTryResult="HandleResult">
    ...
</UAuthLoginForm>
```

The callback receives `IUAuthTryResult`, allowing the same component to report password login and PKCE completion results.

```csharp
private Task HandleResult(IUAuthTryResult result)
{
    if (!result.IsSuccess)
    {
        errorMessage = "Authentication failed.";
    }

    return Task.CompletedTask;
}
```

For flow-specific details, inspect the concrete result type:

```csharp
private Task HandleResult(IUAuthTryResult result)
{
    if (result is TryLoginResult loginResult)
    {
        if (loginResult.LockoutUntilUtc is not null)
        {
            errorMessage = "Your account is temporarily locked.";
        }
        else if (loginResult.RequiresMfa)
        {
            // Continue the supported additional authentication workflow.
        }
        else if (!loginResult.IsSuccess)
        {
            errorMessage = "Unable to sign in.";
        }
    }
    else if (result is TryPkceLoginResult pkceResult)
    {
        if (pkceResult.RetryWithNewPkce)
        {
            errorMessage = "Please restart the authentication flow.";
        }
        else if (!pkceResult.IsSuccess)
        {
            errorMessage = "Unable to complete authentication.";
        }
    }

    return Task.CompletedTask;
}
```

The callback reports the structured try result. It does not replace authentication state management or server-side validation.

### Using UAuthLoginForm with PKCE

The same component also supports PKCE login completion when hosted within a supported UAuthHub environment.

Set `LoginType` to `Pkce`:

```razor
<UAuthLoginForm LoginType="UAuthLoginType.Pkce" Identifier="@email" Secret="@password" SubmitMode="UAuthSubmitMode.TryAndCommit" OnTryResult="HandleLoginResult">
    <input @bind="email" placeholder="Email" />
    <input @bind="password" type="password" placeholder="Password" />
    <button type="submit">Continue</button>
</UAuthLoginForm>
```

In PKCE mode, the component:

1. Resolves the relevant UAuthHub session context.
2. Retrieves the authorization code and code verifier through the configured Hub services.
3. Builds a `PkceCompleteRequest`.
4. Invokes the corresponding PKCE completion flow.
5. Reports the structured result when a try-enabled submission mode is used.

The application does not need to manually construct the PKCE completion request for this form.

**Important:** PKCE mode requires a supported server-side UAuthHub environment. `UAuthLoginForm` checks the available Hub capabilities and throws when PKCE login is requested in an unsupported client profile.

A Blazor WebAssembly application should initiate PKCE using `BeginPkceAsync`; the Hub-hosted login form handles the completion stage.

### Return URL Handling

You can provide an explicit return URL:

```razor
<UAuthLoginForm Identifier="@email" Secret="@password" ReturnUrl="/dashboard">
    ...
</UAuthLoginForm>
```

The component resolves the effective return URL using the following precedence:

1. Explicit `ReturnUrl` parameter.
2. UAuthHub flow return URL when using PKCE.
3. Current navigation URL.

This allows a shared login component to participate in different authentication journeys without hardcoding navigation logic into the form.

### Custom Login Endpoints

For advanced integrations, the `Endpoint` parameter allows the form's HTML action endpoint to be overridden:

```razor
<UAuthLoginForm Identifier="@email" Secret="@password" Endpoint="/custom-login" SubmitMode="UAuthSubmitMode.DirectCommit">
    ...
</UAuthLoginForm>
```

The endpoint is resolved relative to the configured UltimateAuth endpoint base path and multi-tenant settings.

Note that `Endpoint` affects the form's HTML action. Try-based submissions invoke the configured `UAuthClient.Flows` endpoints, so changing `Endpoint` alone does not redirect those SDK requests to a custom endpoint.

### Submit the Form Programmatically

`UAuthLoginForm` exposes `SubmitAsync`, allowing submission through a component reference.

```razor
<UAuthLoginForm @ref="loginForm" Identifier="@email" Secret="@password" AllowEnterKeyToSubmit="false">
    <input @bind="email" placeholder="Email" />
    <input @bind="password" type="password" />
</UAuthLoginForm>

<button type="button" @onclick="SubmitLoginAsync">
    Sign in
</button>

@code {
    private UAuthLoginForm? loginForm;

    private string email = "";
    private string password = "";

    private async Task SubmitLoginAsync()
    {
        if (loginForm is not null)
        {
            await loginForm.SubmitAsync();
        }
    }
}
```

`SubmitAsync` must be called after the component has rendered.

This is useful when submission is initiated from a custom button, dialog, or application-specific interaction.

### Controlling Enter-Key Submission

By default, the component supports Enter-key submission.

You can disable its built-in hidden submit button:

```razor
<UAuthLoginForm Identifier="@email" Secret="@password" AllowEnterKeyToSubmit="false">
    ...
</UAuthLoginForm>
```

This option controls the component's built-in Enter-key submit mechanism. Other submit buttons inside the form may still trigger normal browser form submission behavior.

### Reloading PKCE Context

The component exposes `ReloadAsync` to reload its Hub credentials and flow state.

```razor
<UAuthLoginForm @ref="loginForm" LoginType="UAuthLoginType.Pkce" Identifier="@email" Secret="@password">
    ...
</UAuthLoginForm>

@code {
    private UAuthLoginForm? loginForm;

    private async Task ReloadLoginContextAsync()
    {
        if (loginForm is not null)
        {
            await loginForm.ReloadAsync();
        }
    }
}
```

This is an advanced capability intended for scenarios where the associated Hub state changes and the component must refresh its internal context.

It does not initiate a new PKCE authorization flow by itself.

### Component Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Identifier` | `string?` | `null` | Login identifier |
| `Secret` | `string?` | `null` | Authentication secret |
| `LoginType` | `UAuthLoginType` | `Password` | Password or PKCE login |
| `SubmitMode` | `UAuthSubmitMode` | `TryAndCommit` | Submission strategy |
| `ReturnUrl` | `string?` | `null` | Redirect destination |
| `Endpoint` | `string?` | `null` | Custom HTML form endpoint |
| `HubSessionId` | `HubSessionId?` | `null` | Explicit Hub session context |
| `AllowEnterKeyToSubmit` | `bool` | `true` | Built-in Enter-key submit support |
| `OnTryResult` | `EventCallback<IUAuthTryResult>` | — | Structured try result callback |
| `ChildContent` | `RenderFragment?` | — | Custom form content |

### When to Use UAuthLoginForm

Use `UAuthLoginForm` when building a Blazor login interface that should integrate directly with UltimateAuth's supported authentication flows.

It is particularly useful when:

- You want to use your own component library without implementing the authentication submission logic.
- You need structured login validation feedback.
- You want to switch between try-only and commit behavior.
- You are building a UAuthHub login page that completes PKCE.
- You want to share login UI across supported Blazor hosting environments.

For application-specific workflows that cannot be expressed through the component, use `UAuthClient.Flows` directly.

**The component handles the login workflow, while your application retains control over presentation, user experience, and surrounding application logic.**

## 18. Common Integration Mistakes

### Calling Direct Login from a Public Client

**Problem:** `LoginAsync` throws because direct credential posting is disabled.

**Solution:** Use `BeginPkceAsync` and the supported PKCE completion flow. Do not enable `AllowCredentialPost` to bypass the public-client restriction.

### Treating TryOnly as Authentication

**Problem:** The application receives `IsSuccess = true` from `TryOnly` and assumes the user is logged in.

**Solution:** A successful try result does not commit authentication. Use `TryAndCommit` or the appropriate commit flow.

### Assuming DirectCommit Skips Server Validation

**Problem:** DirectCommit is interpreted as a faster but less secure authentication mode.

**Solution:** DirectCommit skips the separate client try stage. The server still validates the actual authentication request.

### Assuming Every Refresh Rotates Tokens

**Problem:** The application requires `RefreshOutcome.Rotated` for refresh success.

**Solution:** Check `RefreshResult.IsSuccess`. Rotation, touch, and no-op behavior depend on the authentication mode and refresh strategy.

### Using a Session ID for Device Logout

**Problem:** A session identifier is passed to a device logout operation.

**Solution:** `LogoutMyDeviceAsync` requires `LogoutDeviceRequest` with a `SessionChainId`.

### Assuming Logout Immediately Invalidates All JWTs

**Problem:** An application expects every issued JWT access token to become unusable immediately after server-side logout.

**Solution:** Understand the enforcement behavior of SemiHybrid and PureJwt. Local JWT validation may continue accepting previously issued tokens until expiration.

### Using ValidateAsync on Every Render

**Problem:** Components repeatedly make validation requests just to display authenticated UI.

**Solution:** Use the appropriate authentication state integration for UI rendering, and reserve explicit validation for operations that require it.

## Next Steps

- [Session Management](/docs/client/session-management) — Inspect and manage session and device information.
- [Token Security](/docs/security/token-security) — Understand token validity, refresh behavior, and revocation.
- [Refresh Rotation & Replay Protection](/docs/security/refresh-rotation) — Explore refresh concurrency and reuse security.
- [Authentication Modes](/docs/fundamentals/authentication-modes) — Compare authentication behavior across modes.
- [Device Management](/docs/sessions-devices/device-management) — Understand device-associated session chains.