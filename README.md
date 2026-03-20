![UltimateAuth Banner](https://github.com/user-attachments/assets/4204666e-b57a-4cb5-8846-dc7e4f16bfe9)

⚠️ **UltimateAuth is under active development.**

The core architecture and public APIs are now implemented and validated through the sample application.

We are currently polishing the developer experience, reviewing the public client API surface, and preparing the EF Core integration packages.

The first preview release (**v 0.1.0-preview**) is planned within the next week.


![Build](https://github.com/CodeBeamOrg/UltimateAuth/actions/workflows/ultimateauth-ci.yml/badge.svg)
![GitHub stars](https://img.shields.io/github/stars/CodeBeamOrg/UltimateAuth?style=flat&logo=github)
![Last Commit](https://img.shields.io/github/last-commit/CodeBeamOrg/UltimateAuth?branch=dev&logo=github)
![License](https://img.shields.io/github/license/CodeBeamOrg/UltimateAuth)
[![Discord](https://img.shields.io/discord/1459498792192839774?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/QscA86dXSR)
[![codecov](https://codecov.io/gh/CodeBeamOrg/UltimateAuth/branch/dev/graph/badge.svg)](https://codecov.io/gh/CodeBeamOrg/UltimateAuth)


---

UltimateAuth is an open-source authentication framework that unifies secure session and token based authentication, modern PKCE flows, Blazor/Maui-ready client experiences, and a fully extensible architecture — all with a focus on clarity, lightweight design, and developer happiness.

---

## 🌟 Why UltimateAuth: The Six-Point Principles

### **1) Developer-Centric & User-Friendly**
Clean APIs, predictable behavior, minimal ceremony — designed to make authentication *pleasant* for developers.

### **2) Security-Driven**
PKCE, hardened session flows, reuse detection, event-driven safeguards, device awareness, and modern best practices.

### **3) Extensible & Lightweight by Design**
Every component can be replaced or overridden.  
No forced dependencies. No unnecessary weight.

### **4) Plug-and-Play Ready**
From setup to production, UltimateAuth prioritizes a frictionless integration journey with sensible defaults.

### **5) Blazor & MAUI-Ready for Modern .NET**
Blazor WebApp, Blazor WASM, Blazor Server, and .NET MAUI expose weaknesses in traditional auth systems.  
UltimateAuth is engineered from day one to support real-world scenarios across the entire modern .NET UI stack.

### **6) Unified Framework**
One solution, same codebase across Blazor server, WASM and MAUI. UltimateAuth handles client differences internally and providing consistent and reliable public API.

---

# 🚀 Quick Start

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
    .AddUltimateAuthEntityFrameworkCore(); // Production

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
Place this in `App.razor` or `index.html`
```csharp
<script src="_content/CodeBeam.UltimateAuth.Client.Blazor/uauth.min.js"></script>
```

### 5) Optional: Blazor Usings
Add this in `_Imports.razor`
```csharp
@using CodeBeam.UltimateAuth.Client.Blazor
```

### ✅ Done

---

## Usage

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
    var result = await UAuthClient.Flows.LogoutOtherDevicesSelfAsync();
    Console.WriteLine(result.IsSuccess);
}
```

UltimateAuth turns Auth into a simple application service — not a separate system you fight against.
- No manual token handling
- No custom HTTP plumbing
- No fragile redirect logic
- All built-in with extensible options.

---


## 📅 Release Timeline (Targeted)

> _Dates reflect targeted milestones and may evolve with community feedback._

### **Q1 2026 — First Release**
- v 0.1.0-preview to v 0.1.0

### **Q2 2026 — Stable Feature Releases**
- v 0.2.0 to v 0.3.0

### **Q3 2026 — General Availability**
- API surface locked  
- Production-ready security hardening  
- Unified architecture finalized

### **Q4 2026 — v 11.x.x (.NET 11 Alignment Release)**
UltimateAuth adopts .NET platform versioning to align with the broader ecosystem.

---

## 🗺 Roadmap

The project roadmap is actively maintained as a GitHub issue:

👉 https://github.com/CodeBeamOrg/UltimateAuth/issues/8

We keep it up-to-date with current priorities, planned features, and progress.

Feel free to follow, comment, or contribute ideas.

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

## 🛠 Project Status

UltimateAuth core architecture is implemented and validated through the sample application.

We are currently:

- Polishing developer experience
- Reviewing public APIs
- Preparing EF Core integration packages

Preview release is coming soon.

You can check the samples and try what UltimateAuth offers by downloading repo and running locally.

---

## ⭐ Acknowledgements

UltimateAuth is built with love by CodeBeam and shaped by real-world .NET development —  
for teams who want authentication to be secure, predictable, extensible, and a joy to use.

Reimagine how .NET does authentication.  
Welcome to UltimateAuth.
