---
title: QuickStart
order: 3
group: getting-started
---

# ⚡ QuickStart

**Your first UltimateAuth application, in just a few steps.**

This guide walks you through creating a Blazor Server application, adding UltimateAuth with in-memory storage, and signing in with a sample account.

You'll start with a minimal setup. No database configuration, migrations, or custom authentication infrastructure required.

## Prerequisites

You'll need:

- .NET SDK (8 and higher, latest recommended)
- A code editor such as Visual Studio, VS Code, or Rider
- Basic familiarity with Blazor

This guide uses **Blazor Web App with Interactive Server rendering** and UltimateAuth's in-memory persistence bundle.


## 1. Create a Blazor Application

Open a terminal and create a new Blazor Interactive Server project:

```bash
dotnet new blazor -n UltimateAuthDemo -int Server -ai
cd UltimateAuthDemo
```

This creates a Blazor Web App with Interactive Server components.


## 2. Install UltimateAuth

Install the Blazor client adapter and the in-memory bundle:

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle
```

For this tutorial, also install the sample seed package:

```bash
dotnet add package CodeBeam.UltimateAuth.Sample.Seed
```

The packages serve different purposes:

| Package | Purpose |
|---|---|
| `Client.Blazor` | Blazor components, authentication state, and client integration |
| `InMemory.Bundle` | In-memory implementations for authentication domains |
| `Sample.Seed` | Development accounts and sample authorization data |

The in-memory bundle includes the relevant persistence implementations, so you do not need to register each store individually.


## 3. Generate and Store Cryptographic Keys

UltimateAuth uses cryptographic keys to protect authentication credentials.

Even when using PureOpaque authentication, applications should configure their token-hashing key explicitly. JWT-enabled modes additionally require a signing key.

Never embed cryptographic keys in source code, commit them to a repository, or rely on shared development keys.

### Generate independent keys

Generate two independent 256-bit keys using a cryptographically secure random number generator.

In PowerShell:

```powershell
$tokenHashingKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

$jwtSigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Each command generates a different random key.

### Store keys using .NET User Secrets

Initialize User Secrets for your application:

```bash
dotnet user-secrets init
```

Store the generated keys:

```powershell
dotnet user-secrets set "UltimateAuth:Cryptography:TokenHashingKey" $tokenHashingKey
dotnet user-secrets set "UltimateAuth:Cryptography:JwtSigningKey" $jwtSigningKey
```

User Secrets are intended for local development. They are not a production secret-management system.

For production deployments, use environment variables or a dedicated secret manager.

Never expose these keys to browser-based clients, including Blazor WebAssembly.

### Why two keys?

`TokenHashingKey` protects server-managed token hashes using HMAC-SHA256.

`JwtSigningKey` signs JWT access tokens in JWT-enabled authentication modes.

The keys serve different security purposes and must be independently generated.

The QuickStart uses PureOpaque by default, but configures both keys so that the application does not depend on built-in development key material.


## 4. Configure UltimateAuth Services

Open `Program.cs`.

Add the following namespaces:

```csharp
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Sample.Seed.Extensions;
using CodeBeam.UltimateAuth.Server.Extensions;
```

After creating the builder, register UltimateAuth:

```csharp

static string RequireSecret(IConfiguration configuration, string name)
{
    var value = configuration[name];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Required UltimateAuth secret '{name}' is missing.");
    }

    return value;
}

builder.Services.AddUltimateAuthServer(options =>
{
    options.Cryptography.TokenHashingKey = RequireSecret(builder.Configuration,
        "UltimateAuth:Cryptography:TokenHashingKey");

    // Not required for PureOpaque, but needed for JWT-enabled modes.
    options.Cryptography.JwtSigningKey = RequireSecret(builder.Configuration,
        "UltimateAuth:Cryptography:JwtSigningKey");
})
.AddUltimateAuthInMemory();

builder.Services.AddUltimateAuthClientBlazor();
builder.Services.AddUltimateAuthSampleSeed();

```

That's the core service configuration.

`AddUltimateAuthServer()` registers the authentication server.

`AddUltimateAuthInMemory()` provides in-memory stores.

`AddUltimateAuthClientBlazor()` registers the Blazor client integration.

`AddUltimateAuthSampleSeed()` prepares the development seed services.

You don't need to call `AddUltimateAuth()` or `AddUltimateAuthClient()` separately.

## 5. Configure the HTTP Pipeline

In `Program.cs`, configure the application pipeline:

```csharp
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();
app.MapUltimateAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());

if (app.Environment.IsDevelopment())
{
    await app.SeedUltimateAuthAsync();
}

app.Run();
```

Keep the `AddRazorComponents().AddInteractiveServerComponents()` registration generated by the Blazor project template.

`UseUltimateAuthWithAspNetCore()` integrates UltimateAuth's middleware with ASP.NET Core authentication and authorization.

`MapUltimateAuthEndpoints()` registers the authentication endpoints.

`AddUltimateAuthRoutes()` includes UltimateAuth's Blazor client route assemblies in endpoint discovery.

The development-only seed call creates the sample users before the application begins accepting requests.

## 6. Add the UltimateAuth Script

Open `Components/App.razor`.

Keep the existing Blazor script and add UltimateAuth's JavaScript file:

```html
<script src="_content/CodeBeam.UltimateAuth.Client.Blazor/uauth.min.js"></script>
```

Place these near the end of the document body.

UltimateAuth's Blazor integration uses this script for browser-side functionality.

## 7. Add Usings

Add this in `_Imports.razor`

```razor
@using CodeBeam.UltimateAuth.Client
@using CodeBeam.UltimateAuth.Client.Blazor
```

## 8. Initialize the UltimateAuth Application

Open `Components/Routes.razor`.

Replace its contents with:

```razor
<UAuthApp UseBuiltInRouter="true" AppAssembly="typeof(Program).Assembly" DefaultLayout="typeof(Layout.MainLayout)" />
```

`UAuthApp` is the root integration component for UltimateAuth in Blazor.

It initializes the client runtime, provides authentication state to child components, and coordinates the authenticated session lifecycle.

With `UseBuiltInRouter="true"`, it also provides the application's router.

You don't need to create a separate `Router` or `CascadingAuthenticationState` component for this setup.

## 9. Create Your Login Page

Change `Components/Pages/Home.razor`:

```razor
@page "/"
@page "/login"
@attribute [UAuthLoginPage]
@inherits UAuthPageBase

<div class="container mt-5" style="max-width: 400px;">
    <h3>Sign in</h3>
    <UAuthLoginForm ReturnUrl="/dashboard">
        <div class="d-block mb-3">
            <label>Username</label>
            <input name="Identifier" class="form-control w-100" autocomplete="username" />
        </div>

        <div class="d-block mb-3">
            <label>Password</label>
            <input type="password" name="Secret" class="form-control w-100" autocomplete="current-password" />
        </div>

        <button class="btn btn-primary w-100" type="submit">Sign in</button>
    </UAuthLoginForm>
</div>
```

`UAuthLoginForm` handles the login submission through UltimateAuth's client flow.

`[UAuthLoginPage]` identifies this component as the application's login page.

Only designate one login page for this purpose.

The form above is intentionally minimal. You can replace the inputs with your preferred UI components later.

## 10. Add a Protected Page

Create `Components/Pages/Dashboard.razor`:

```razor
@page "/dashboard"
@attribute [UAuthAuthorize]
@inherits UAuthPageBase
@inject IUAuthClient UAuthClient

<h3>Dashboard</h3>

<p>You have reached an authenticated page.</p>
<UAuthStateView Roles="Admin">You have admin privileges.</UAuthStateView>
<p>User: @AuthState?.Identity?.DisplayName</p>

<button @onclick="Logout" class="btn btn-primary">Logout</button>

@code {
    private async Task Logout()
    {
        await UAuthClient.Flows.LogoutAsync();
    }
}
```

`[UAuthAuthorize]` declares an UltimateAuth authorization requirement for the page.

**Important:** Client-side route protection is not a substitute for server-side authorization. Protected APIs and resources must enforce authorization on the server.

## 11. Run the Application

Start the application:

```bash
dotnet run
```

Open the HTTPS address displayed in your terminal, then navigate to:

```text
/login
```

The sample seed package creates two development accounts:

| Username | Password | Role |
|---|---|---|
| `admin` | `admin` | Administrator |
| `user` | `user` | Standard user |

Sign in with:

```text
Username: admin
Password: admin
```

After successful authentication, navigate to:

```text
/dashboard
```

You now have a Blazor application using UltimateAuth authentication.

## 🎉 Your First UltimateAuth Application Is Running

With this setup, you have:

- An authentication server integrated into ASP.NET Core.
- In-memory user, credential, session, token, and authorization storage.
- A Blazor authentication client.
- A working login form.
- Authentication state initialization.
- Session lifecycle coordination.
- Sample users and authorization data.
- A page declaring an authentication requirement.

You did not need to create your own password hashing, token issuance, session validation, or authentication middleware implementations.

## What Happened When You Logged In?

For this Blazor Server scenario, UltimateAuth uses the applicable session-oriented authentication model.

The login flow validates the credentials and establishes authentication state on the server.

Where a new session is created, it participates in UltimateAuth's root, chain, and session lifecycle.

The Blazor client then coordinates authentication state for the application.

You can explore these operations in detail later through the Authentication Flows and Session Management documentation.

## Important: In-Memory Storage

This QuickStart intentionally uses in-memory persistence.

It is ideal for:

- Learning UltimateAuth.
- Local development.
- Testing authentication flows.
- Building prototypes.

**In-memory authentication data is not durable.** Users, credentials, sessions, and other stored state may be lost when the application restarts.

The sample credentials are also for development only. Do not deploy the seeded accounts or their example passwords in a production application.

**Cryptographic keys are independent from persistence.** Using in-memory storage does not remove the need for securely generated cryptographic keys.

Keep cryptographic secrets outside source control. Do not replace missing keys with hardcoded fallback values.

For production, use durable persistence and a secure secret-management solution. Preserve required cryptographic keys across application restarts and deployments; changing a key can invalidate credentials that depend on it.

## Next Steps

- **[Ecosystem](/docs/getting-started/ecosystem)** — Explore packages, integrations, and supported environments.
- **[Server Options](/docs/configuration/server-options)** — Configure sessions, tokens, login policies, and security behavior.
- **[Client Options](/docs/configuration/client-options)** — Customize Blazor integration and authentication flows.
- **[Authentication Flows](/docs/auth-flows/overview)** — Understand login, refresh, logout, and revocation.
- **[Entity Framework Core Setup](/docs/getting-started/real-world-setup#persistent-storage-with-entity-framework-core)** — Move from in-memory storage to persistent authentication data.