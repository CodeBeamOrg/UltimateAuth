<p align="center">
  <img src="https://github.com/user-attachments/assets/c8f3e1bc-3a85-432f-b48a-fda9984d3967" style="width: 800px; max-width: 100%;" />
</p>

<br /><br />

![Build](https://github.com/CodeBeamOrg/UltimateAuth/actions/workflows/ultimateauth-ci.yml/badge.svg)
![GitHub stars](https://img.shields.io/github/stars/CodeBeamOrg/UltimateAuth?style=flat&logo=github)
![Last Commit](https://img.shields.io/github/last-commit/CodeBeamOrg/UltimateAuth?branch=dev&logo=github)
![License](https://img.shields.io/github/license/CodeBeamOrg/UltimateAuth)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/QscA86dXSR)
[![codecov](https://codecov.io/gh/CodeBeamOrg/UltimateAuth/branch/dev/graph/badge.svg)](https://codecov.io/gh/CodeBeamOrg/UltimateAuth)
[![NuGet version](https://img.shields.io/nuget/v/CodeBeam.UltimateAuth.Core?color=512bd4&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/CodeBeam.UltimateAuth.Core)
[![NuGet downloads](https://img.shields.io/nuget/dt/CodeBeam.UltimateAuth.Core?color=512bd4&label=nuget%20downloads&logo=nuget&style=flat-square)](https://www.nuget.org/packages/CodeBeam.UltimateAuth.Core)
<!-- Dynamic Discord badge (enable when community grows)
[![Discord](https://img.shields.io/discord/1459498792192839774?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/QscA86dXSR)
-->


## 📑 Table of Contents

## Table of Contents


## Table of Contents

- [Roadmap](#roadmap)
- [Why UltimateAuth](#why-ultimateauth)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [Documentation](#documentation)
- [Upgrading & Breaking Changes](#upgrading--breaking-changes)
- [Community & Contributing](#community--contributing)
- [Acknowledgements](#acknowledgements)

---

UltimateAuth is an open-source auth framework with platform-level capabilities that unifies secure session, cookie and token based Auth, modern PKCE flows, Blazor/Maui-ready client experiences - eliminating the complexity of traditional Auth systems while providing a clean, lightweight, extensible and developer-first architecture.

---
## Roadmap

| Phase                   | Version       | Scope                                     | Status         | Release Date  |
| ----------------------- | ------------- | ----------------------------------------- | -------------- | ------------  |
| First Preview           | 0.1.0-preview | "Stable" Preview Core                     | ✅ Completed   | 07.04.2026    |
| First Release*          | 0.1.0         | Documented & Quality Tested (Client Public API Locked)               | ✅ Completed   | 12.10.2026    |
| Product Expansion       | 0.2.0         | Full Auth Modes                           | 🟡 In Progress | Q4 2026       |
| Security Expansion      | 0.3.0         | MFA, Reauth, Rate Limiting                | 🟡 In Progress | Q4 2026       |
| Infrastructure Expansion| 0.4.0         | Redis, Distributed Cache, Password Hasher | 🔜 Planned     | Q1 2027       |
| Multi-Tenant Expansion  | 0.5.0         | Multi tenant management                   | 🔜 Planned     | Q1 2027       |
| Extensibility Expansion | 0.6.0         | Audit, events, hooks                      | 🔜 Planned     | Q1 2027       |
| Performance Expansion   | 0.7.0         | Benchmarks, caching                       | 🔜 Planned     | Q1 2027       |
| Ecosystem Expansion     | 0.8.0         | Migration tools                           | 🔜 Planned     | Q2 2027       |
| v1.0                    | 1.0.0         | Locked API, align with .NET 11            | 🔜 Planned     | Q2 2027       |

*v 0.1.0 already provides a skeleton and extensibility points of multi tenancy, MFA, reauth etc. Expansion releases will enhance these areas.

> The project roadmap is actively maintained as a GitHub issue:

👉 https://github.com/CodeBeamOrg/UltimateAuth/issues/8

We keep it up-to-date with current priorities, planned features, and progress. Feel free to follow, comment, or contribute ideas.

---

### 💜 Join the UltimateAuth Community

UltimateAuth is more than a framework — it's a growing open-source community.

Join our **[Discord server](https://discord.gg/QscA86dXSR)** to connect with developers, ask questions, share ideas, discuss authentication challenges, and help shape the future of UltimateAuth.

Whether you're using UltimateAuth, exploring the project, or interested in contributing, **you're always welcome!**

---

## Why UltimateAuth
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

# Quick Start
> ⏱ Get started in minutes
>
> **This Quick Start uses a Blazor Server application with in-memory persistence.**
It is intentionally designed as the simplest path to a working UltimateAuth application.

> For Entity Framework Core, Blazor WebAssembly, Blazor Web App, Resource API, persistent storage, and other real-world configurations, see the [Real-World Setup guide](https://github.com/CodeBeamOrg/UltimateAuth/blob/dev/docs/content/getting-started/real-world-setup.md).

UltimateAuth also provides ready-to-use login components, authentication state management, session handling, and a unified client API.

> **This Quick Start provides a simplified integration overview.**
> For a complete, copy-paste-ready Blazor Server application,
> see the [Full Quick Start Guide](https://ultimateauth.com/docs/getting-started/quickstart)
> or
> [GitHub Quick Start Page](https://github.com/CodeBeamOrg/UltimateAuth/blob/dev/docs/content/getting-started/quickstart.md).

### 1. Install

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle
```

### 2. Register services

```csharp
builder.Services.AddUltimateAuthServer()
    .AddUltimateAuthInMemory();

builder.Services.AddUltimateAuthClientBlazor();
```

**Usage by application type:**

- **Blazor Server App / UAuthHub (Auth Server)** → Use both Server and Client registrations  
- **Blazor WASM / MAUI** → Use Client only  
- **Resource API** → Use own ResourceApi registration

### 3. Configure the pipeline

```csharp
app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();
app.MapUltimateAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
```

### 4. Add UAuthApp
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

### 5. Add the UltimateAuth Client Script
Place this in `App.razor` or `index.html` in your Blazor client application:
```html
<script src="_content/CodeBeam.UltimateAuth.Client.Blazor/uauth.min.js"></script>
```

### 6. Create Your Login Page

Change `Components/Pages/Home.razor`:

```razor
@page "/"
@page "/login"
@attribute [UAuthLoginPage]
@inherits UAuthPageBase

<div class="container mt-5" style="max-width: 400px;">
    <h3>Sign in</h3>
    <UAuthLoginForm Identifier="@_identifier" Secret="@_password" ReturnUrl="/dashboard">
        <div class="d-block mb-3">
            <label>Username</label>
            <input @bind="_identifier" class="form-control w-100" autocomplete="username" />
        </div>

        <div class="d-block mb-3">
            <label>Password</label>
            <input type="password" @bind="_password" class="form-control w-100" autocomplete="current-password" />
        </div>

        <button class="btn btn-primary w-100" type="submit">Sign in</button>
    </UAuthLoginForm>
</div>

@code {
    private string _identifier = "";
    private string _password = "";
}
```

### 7. Protect your pages

```razor
@page "/dashboard"
@attribute [UAuthAuthorize]
@inherits UAuthPageBase

<h3>Dashboard</h3>
<p>Welcome, @AuthState?.Identity?.DisplayName!</p>
```

The examples above provide a simplified overview of UltimateAuth integration and are not intended to be a complete, copy-paste-ready application.

For a **complete, step-by-step setup with all required configuration, components, and sample data**, follow the full Quick Start guide.

**[⚡ Complete Quick Start — Build a Working Application](https://ultimateauth.com/docs/getting-started/quickstart)**

---

## Usage

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

## Documentation

Two documentation experiences are provided:

### **1) Classic Documentation**
Guides, API reference, tutorials - https://ultimateauth.com

### **2) Interactive Identity Sandbox** (Available Soon)
Create accounts, simulate devices, test auth flows, and observe UltimateAuth in action.  

---

## Upgrading & Breaking Changes

UltimateAuth aims to keep upgrades straightforward. When a release introduces breaking changes, we provide concise migration guides explaining what needs to be updated.

**Upgrading from an earlier version?**

Check the [Migration Guides](https://ultimateauth.com/docs/migration/index) for version-specific instructions.

For new features and improvements, see the [GitHub Releases](https://github.com/CodeBeamOrg/UltimateAuth/releases).

---

## Community & Contributing

UltimateAuth is built by developers, for developers — and everyone is welcome to be part of its journey.

Whether you're reporting a bug, proposing an idea, improving documentation, or submitting code, your contribution matters.

**You don't need to be an expert to get involved.**

- 💜 **[Join our Discord](https://discord.gg/QscA86dXSR)** — Connect with the community and share ideas.
- 🤝 **[Contributing Guide](CONTRIBUTING.md)** — Learn how to contribute.
- 🛡️ **[Security Policy](SECURITY.md)** — Report security vulnerabilities responsibly.
- 📜 **[Code of Conduct](CODE_OF_CONDUCT.md)** — Our community values.
- 🏛️ **[Governance](GOVERNANCE.md)** — How project decisions are made.

Every great open-source project starts with people willing to build something together.

---

## Acknowledgements

UltimateAuth is built with love by CodeBeam and shaped by real-world .NET development —  
for teams who want authentication to be secure, predictable, extensible, and a joy to use.

Reimagine how .NET does authentication.  
Welcome to UltimateAuth.
