<p align="center">
  <img src="https://github.com/user-attachments/assets/c8f3e1bc-3a85-432f-b48a-fda9984d3967" style="width: 800px; max-width: 100%;" />
</p>

<br /><br />

![Build](https://github.com/CodeBeamOrg/UltimateAuth/actions/workflows/ultimateauth-ci.yml/badge.svg)
![GitHub stars](https://img.shields.io/github/stars/CodeBeamOrg/UltimateAuth?style=flat&logo=github)
![Last Commit](https://img.shields.io/github/last-commit/CodeBeamOrg/UltimateAuth?branch=dev&logo=github)
![License](https://img.shields.io/github/license/CodeBeamOrg/UltimateAuth)
[![Discord](https://img.shields.io/discord/1459498792192839774?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/QscA86dXSR)
[![codecov](https://codecov.io/gh/CodeBeamOrg/UltimateAuth/branch/dev/graph/badge.svg)](https://codecov.io/gh/CodeBeamOrg/UltimateAuth)
[![NuGet version](https://img.shields.io/nuget/v/CodeBeam.UltimateAuth.Core?color=512bd4&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/CodeBeam.UltimateAuth.Core)
[![NuGet downloads](https://img.shields.io/nuget/dt/CodeBeam.UltimateAuth.Core?color=512bd4&label=nuget%20downloads&logo=nuget&style=flat-square)](https://www.nuget.org/packages/CodeBeam.UltimateAuth.Core)

## 📑 Table of Contents

- [🗺 Roadmap](#-roadmap)
- [🌟 Why UltimateAuth](#-why-ultimateauth)
- [🚀 Quick Start](#-quick-start)
- [💡 Usage](#-usage)
- [📘 Documentation](#-documentation)
- [🤝 Contributing](#-contributing)
- [⭐ Acknowledgements](#-acknowledgements)

---

UltimateAuth is an open-source auth framework with platform-level capabilities that unifies secure session, cookie and token based Auth, modern PKCE flows, Blazor/Maui-ready client experiences - eliminating the complexity of traditional Auth systems while providing a clean, lightweight, extensible and developer-first architecture.

---
## 🗺 Roadmap

| Phase                   | Version       | Scope                                     | Status         | Release Date  |
| ----------------------- | ------------- | ----------------------------------------- | -------------- | ------------  |
| First Preview           | 0.1.0-preview | "Stable" Preview Core                     | ✅ Completed   | 07.04.2026    |
| First Release*          | 0.1.0         | Fully Documented & Quality Tested         | ✅ Completed   | 04.10.2026    |
| Product Expansion       | 0.2.0         | Full Auth Modes                           | 🟡 In Progress | Q4 2026       |
| Security Expansion      | 0.3.0         | MFA, Reauth, Rate Limiting                | 🟡 In Progress | Q4 2026       |
| Infrastructure Expansion| 0.4.0         | Redis, Distributed Cache, Password Hasher | 🔜 Planned     | Q1 2027       |
| Multi-Tenant Expansion  | 0.5.0         | Multi tenant management                   | 🔜 Planned     | Q1 2027       |
| Extensibility Expansion | 0.6.0         | Audit, events, hooks                      | 🔜 Planned     | Q1 2027       |
| Performance Expansion   | 0.7.0         | Benchmarks, caching                       | 🔜 Planned     | Q1 2027       |
| Ecosystem Expansion     | 0.8.0         | Migration tools                           | 🔜 Planned     | Q2 2027       |
| v1.0                    | 1.0.0         | Locked API, align with .NET 11            | 🔜 Planned     | Q2 2027       |

*v 0.1.0 already provides a skeleton of multi tenancy, MFA, reauth etc. Expansion releases will enhance these areas.

> The project roadmap is actively maintained as a GitHub issue:

👉 https://github.com/CodeBeamOrg/UltimateAuth/issues/8

We keep it up-to-date with current priorities, planned features, and progress. Feel free to follow, comment, or contribute ideas.

---

## 🌟 Why UltimateAuth
The Six-Point Principles

### 1) Unified Authentication System

One solution, one mental model — across Blazor Server, WASM, MAUI, and APIs.
UltimateAuth eliminates fragmentation by handling client differences internally and exposing a single, consistent API.

### 2) Plug & Play Ready

Built-in capabilities designed for real-world scenarios:

- Automatic client profile detection (blazor server - WASM - MAUI)
- Selectable authentication modes (Session / Token / Hybrid / SemiHybrid)
- Device-aware sessions
- PKCE flows out of the box
- Unified session + token lifecycle
- Event-driven extensibility

No boilerplate. No hidden complexity.

### 3) Developer-Centric

Clean APIs, predictable behavior, minimal ceremony — designed to make authentication pleasant.

### 4) Security as a First-Class Concern

Modern security built-in by default:

- PKCE support
- Session reuse detection
- Device tracking
- Hardened auth flows
- Safe defaults

### 5) Extensible & Lightweight

Start simple, scale infinitely:

- Works out of the box with sensible defaults
- Replace any component when needed
- No forced architecture decisions

### 6) Built for Modern .NET Applications

Designed specifically for real-world .NET environments:

- Blazor Server
- Blazor WASM
- Blazor Web App
- .NET MAUI & Hybrid Apps
- Backend APIs

Traditional auth solutions struggle here — UltimateAuth embraces it.

---

# 🚀 Quick Start
> ⏱ Takes ~2 minutes to get started
>
> **This Quick Start uses a Blazor Server application with in-memory persistence.**
It is intentionally designed as the simplest path to a working UltimateAuth application.

> For Entity Framework Core, Blazor WebAssembly, Blazor Web App, Resource API, persistent storage, and other real-world configurations, see the [Real-World Setup guide](https://github.com/CodeBeamOrg/UltimateAuth/blob/dev/docs/content/getting-started/real-world-setup.md).

### 1) Install UltimateAuth

```bash
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

### 2) Configure UltimateAuth

Register UltimateAuth in `Program.cs`:

```csharp
// Server registration
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthInMemory();

// Client registration
builder.Services.AddUltimateAuthClientBlazor();
```


**Usage by application type:**

- **Blazor Server App** → Use both Server and Client registrations  
- **Blazor WASM / MAUI** → Use Client only  
- **UAuthHub (Auth Server) / Resource API** → Use Server only

### 3) Configure the Application Pipeline
Add the UltimateAuth middleware and endpoints:

```csharp
// app.UseHttpsRedirection();
// app.UseStaticFiles();

app.UseUltimateAuthWithAspNetCore(); // Includes UseAuthentication() and UseAuthorization()
// Place Antiforgery or something else before endpoint registration if needed
app.MapUltimateAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
```

### 4) Add UAuthApp
UltimateAuth uses `UAuthApp` as the root integration point for its client authentication state and Blazor lifecycle.

Replace the default router in your `App.razor` or `Routes.razor` with:

```razor
@using CodeBeam.UltimateAuth.Client.Blazor

<UAuthApp UseBuiltInRouter="true" AppAssembly="typeof(Program).Assembly" DefaultLayout="typeof(Layout.MainLayout)">
    <ChildContent>
        @* Add application-wide UI providers or other root components here. *@
    </ChildContent>
    
    <NotAuthorized>
        <p>Not authorized.</p>
    </NotAuthorized>
</UAuthApp>
```

`UAuthApp` can provide the built-in router, authentication state, and UltimateAuth client lifecycle integration for your component tree.

> Need full control over routing?

> UAuthApp also supports applications that provide their own Blazor Router. See the Blazor Routing guide for advanced routing configuration.

### 5) Add the UltimateAuth Client Script
Place this in `App.razor` or `index.html` in your Blazor client application:
```html
<script src="_content/CodeBeam.UltimateAuth.Client.Blazor/uauth.min.js"></script>
```

### 6) Optional: Blazor Usings
Add this in `_Imports.razor`:
```csharp
@using CodeBeam.UltimateAuth.Client.Blazor
```

### 7) Optional: Add Sample Data
For the fastest way to try auth process, install the UltimateAuth sample seed package:

```bash
dotnet add package CodeBeam.UltimateAuth.Sample.Seed
```

Register the development seed:

```bash
builder.Services.AddUltimateAuthSampleSeed();
```

Then seed the application during development:

```csharp
if (app.Environment.IsDevelopment())
{
    await app.SeedUltimateAuthAsync();
}
```

The development seed includes ready-to-use accounts:

| Identifier | Secret   |
|------------|----------|
| `admin`    | `admin`  |
| `user`     | `user`   |

You can use these credentials to test the auth flows immediately.

> Development only: Sample users and credentials are intended for evaluation and local development. Do not use them in production.

### ✅ You're Ready

---

## 💡 Usage

**One Client. Your Auth Application API.**

For most application-level authentication and identity operations, start with `IUAuthClient`.

`IUAuthClient` provides a single entry point to UltimateAuth capabilities such as authentication flows, users, sessions, tokens, profiles, credentials, and authorization — without requiring your application code to manage the underlying authentication transport.

> UltimateAuth treats authentication and identity as application services. Your application works with explicit operations and structured results while UltimateAuth handles the underlying authentication flow.

### Examples
Login
```csharp
[Inject] IUAuthClient UAuthClient { get; set; } = null!;

private async Task Login()
{
    var request = new LoginRequest
    {
        Identifier = "admin",
        Secret = "admin",
    };
    await UAuthClient.Flows.LoginAsync(request);
}
```

Register
```csharp
[Inject] IUAuthClient UAuthClient { get; set; } = null!;

private async Task Register()
{
    var request = new CreateUserRequest
    {
        UserName = "NewUser",
        Password = "NewUserPassword",
        Email = "newuser@example.com",
    };

    var result = await UAuthClient.Users.CreateAsync(request);
    if (result.IsSuccess)
    {
        Console.WriteLine("User created successfully.");
    }
    else
    {
        Console.WriteLine(result.ErrorText ?? "Failed to create user.");
    }
}
```

LogoutAll But Keep Current Device
```csharp
[Inject] IUAuthClient UAuthClient { get; set; } = null!;

private async Task LogoutOthersAsync()
{
    var result = await UAuthClient.Flows.LogoutMyOtherDevicesAsync();
    Console.WriteLine(result.IsSuccess);
}
```

With `IUAuthClient`, common application code doesn't need to manually orchestrate:
- token handling
- authentication HTTP calls
- session operations
- redirect plumbing
- client-specific authentication flows

Start with the simple API. Drop down to UltimateAuth's extensibility points when your application needs more control.

---

## 📘 Documentation

Two documentation experiences are provided:

### **1) Classic Documentation**
Guides, API reference, tutorials - https://ultimateauth.com

### **2) Interactive Identity Sandbox** (Available Soon)
Create accounts, simulate devices, test auth flows, and observe UltimateAuth in action.  

---

## 🤝 Contributing

UltimateAuth is a community-first framework.  
We welcome proposals, discussions, architectural insights, and contributions of all sizes.

Discussions are open — your ideas matter.

---

## ⭐ Acknowledgements

UltimateAuth is built with love by CodeBeam and shaped by real-world .NET development —  
for teams who want authentication to be secure, predictable, extensible, and a joy to use.

Reimagine how .NET does authentication.  
Welcome to UltimateAuth.
