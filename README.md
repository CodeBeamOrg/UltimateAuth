![UltimateAuth Banner](https://github.com/user-attachments/assets/4204666e-b57a-4cb5-8846-dc7e4f16bfe9)

![Build](https://github.com/CodeBeamOrg/UltimateAuth/actions/workflows/ultimateauth-ci.yml/badge.svg)
![GitHub stars](https://img.shields.io/github/stars/CodeBeamOrg/UltimateAuth?style=flat&logo=github)
![Last Commit](https://img.shields.io/github/last-commit/CodeBeamOrg/UltimateAuth?branch=dev&logo=github)
![License](https://img.shields.io/github/license/CodeBeamOrg/UltimateAuth)
[![Discord](https://img.shields.io/discord/1459498792192839774?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/QscA86dXSR)
[![codecov](https://codecov.io/gh/CodeBeamOrg/UltimateAuth/branch/dev/graph/badge.svg)](https://codecov.io/gh/CodeBeamOrg/UltimateAuth)

---

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
| First Release*          | 0.1.0         | Fully Documented & Quality Tested         | 🟡 In Progress | Q2 2026       |
| Product Expansion       | 0.2.0         | Full Auth Modes                           | 🟡 In Progress | Q2 2026       |
| Security Expansion      | 0.3.0         | MFA, Reauth, Rate Limiting                | 🔜 Planned     | Q2 2026       |
| Infrastructure Expansion| 0.4.0         | Redis, Distributed Cache, Password Hasher | 🔜 Planned     | Q2 2026       |
| Multi-Tenant Expansion  | 0.5.0         | Multi tenant management                   | 🔜 Planned     | Q3 2026       |
| Extensibility Expansion | 0.6.0         | Audit, events, hooks                      | 🔜 Planned     | Q3 2026       |
| Performance Expansion   | 0.7.0         | Benchmarks, caching                       | 🔜 Planned     | Q3 2026       |
| Ecosystem Expansion     | 0.8.0         | Migration tools                           | 🔜 Planned     | Q4 2026       |
| v1.0                    | 1.0.0         | Locked API, align with .NET 11            | 🔜 Planned     | Q4 2026       |

*v 0.1.0 already provides a skeleton of multi tenancy, MFA, reauth etc. Expansion releases will enhance these areas.

> The project roadmap is actively maintained as a GitHub issue:

👉 https://github.com/CodeBeamOrg/UltimateAuth/issues/8

We keep it up-to-date with current priorities, planned features, and progress. Feel free to follow, comment, or contribute ideas.

<details>

> UltimateAuth is currently in the final stage of the first preview release (v 0.1.0-preview).

> Core architecture is complete and validated through working samples.

> Ongoing work:
> - Final API surface review
> - Developer experience improvements
> - EF Core integration polishing
> - Documentation refinement
</details>

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
- Safe defaults with extensibility

### 5) Extensible & Lightweight

Start simple, scale infinitely:

- Works out of the box with sensible defaults
- Replace any component when needed
- No forced architecture decisions

### 6) Built for Modern .NET Applications

Designed specifically for real-world .NET environments:

- Blazor Server
- Blazor WASM
- .NET MAUI
- Backend APIs

Traditional auth solutions struggle here — UltimateAuth embraces it.

---

# 🚀 Quick Start
> ⏱ Takes ~2 minutes to get started

### 1) Install packages (Will be available soon)

1.1 Core Packages
```bash
dotnet add package CodeBeam.UltimateAuth.Server
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```
1.2 Persistence & Reference Packages (Choose One)
```bash
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle (for debug & development)
dotnet add package CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle (for production)
```
### 2) Configure services (in program.cs)
Server registration:
```csharp
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        // use with your database provider
        db.UseSqlite("Data Source=uauth.db");
    });

// OR

builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthInMemory(); // Development

```

Client registration:

```csharp
builder.Services.AddUltimateAuthClientBlazor();
```
**Usage by application type:**

- **Blazor Server App** → Use both Server and Client registrations  
- **Blazor WASM / MAUI** → Use Client only  
- **Auth Server / Resource API** → Use Server only

### 3) Configure pipeline
```csharp
// app.UseHttpsRedirection();
// app.UseStaticFiles();

app.UseUltimateAuthWithAspNetCore(); // Includes UseAuthentication() and UseAuthorization()
// Place Antiforgery or something else needed
app.MapUltimateAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
```

### 4) Add UAuth Script
Place this in `App.razor` or `index.html` in your Blazor client application:
```csharp
<script src="_content/CodeBeam.UltimateAuth.Client.Blazor/uauth.min.js"></script>
```

### 5) 🗄️ Database Setup (EF Core)

After configuring UltimateAuth with Entity Framework Core, you need to create and apply database migrations.

5.1) Install EF Core tools (if not installed)
```bash
dotnet tool install --global dotnet-ef
```
5.2) Add migration
```bash
dotnet ef migrations add InitUAuth
```

5.3) Update database
```bash
dotnet ef database update
```
💡 Visual Studio (PMC alternative)

If you are using Visual Studio, you can run these commands in Package Manager Console:
```bash
Add-Migration InitUAuth -Context UAuthDbContext
Update-Database -Context UAuthDbContext
```
⚠️ Notes
- Migrations must be created in your application project, not in the UltimateAuth packages
- You are responsible for managing migrations in production
- Automatic database initialization is not enabled by default

### 6) Optional: Blazor Usings
Add this in `_Imports.razor`
```csharp
@using CodeBeam.UltimateAuth.Client.Blazor
```

### ✅ Done

---

## 💡 Usage

Inject IUAuthClient and simply call methods.

### Examples
Login
```csharp
[Inject] IUAuthClient UAuthClient { get; set; } = null!;

private async Task Login()
{
    var request = new LoginRequest
    {
        Identifier = "UAuthUser",
        Secret = "UAuthPassword",
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
        UserName = _username,
        Password = _password,
        Email = _email,
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

UltimateAuth turns Auth into a simple application service — not a separate system you fight against.
- No manual token handling
- No custom HTTP plumbing
- No fragile redirect logic
- All built-in with extensible options.

---

## 📘 Documentation

Two documentation experiences will be provided:

### **1) Classic Documentation**
Guides, API reference, tutorials  

### **2) Interactive Identity Sandbox**
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
