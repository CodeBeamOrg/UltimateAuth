---
title: Advanced Configuration
order: 6
group: configuration
---


# 🧠 Advanced Configuration

UltimateAuth is designed to be flexible — but not fragile.

👉 You can customize almost every part of the system  
👉 Without breaking its security guarantees

## ⚠️ Philosophy

Customization in UltimateAuth follows one rule:

👉 You can extend behavior  
👉 You should not bypass security

<br>

## 🧩 Extension Points

UltimateAuth exposes multiple extension points:

- Resolvers  
- Validators  
- Authorities  
- Orchestrators  
- Stores  
- Events  

👉 You don’t replace the system  
👉 You plug into it

<br>

## 🔌 Replacing Services

All core services can be overridden using DI:

```csharp
services.AddScoped<ISessionValidator, CustomSessionValidator>();
```

👉 This allows deep customization  
👉 While preserving the pipeline

<br>

## 🧠 Authorities & Decisions

Authorities are responsible for decisions:

- LoginAuthority  
- AccessAuthority  

---

You can override them:

```csharp
services.AddScoped<ILoginAuthority, CustomLoginAuthority>();
```

👉 This changes decision logic  
👉 Without touching flows

<br>

## 🔄 Orchestrators

Orchestrators coordinate execution:

- Validate  
- Authorize  
- Execute command  

👉 They enforce invariants  
👉 Do not bypass them, unless you exact know what you are doing

<br>

## 🗄 Store Customization

You can provide custom stores:

- Session store  
- Refresh token store  
- User store  

👉 Supports EF Core, in-memory, or custom implementations

## 📡 Events

UltimateAuth provides event hooks:

- Login  
- Logout  
- Refresh  
- Revoke  

---

```csharp
o.Events.OnUserLoggedIn = ctx =>
{
    // custom logic
    return Task.CompletedTask;
};
```

👉 Use events for side-effects  
👉 Not for core logic

<br>

## ⚙️ Mode Configuration Overrides

You can customize behavior per mode:

```csharp
o.ModeConfigurations[UAuthMode.Hybrid] = options =>
{
    options.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
};
```

👉 Runs after defaults  
👉 Allows fine-grained control

<br>

## 🔐 Custom Resolvers

You can override how data is resolved:

- Tenant resolver  
- Session resolver  
- Device resolver  

👉 Enables full control over request interpretation

## 🛡 Safety Boundaries

UltimateAuth enforces:

- Invariants  
- Validation  
- Fail-fast behavior  

👉 Unsafe overrides will fail early

## 🧠 Mental Model

If you remember one thing:

👉 Extend the system  
👉 Don’t fight the system

---

## 📌 Key Takeaways

- Everything is replaceable via DI  
- Authorities control decisions  
- Orchestrators enforce flow  
- Events are for side-effects  
- Security boundaries are protected  

---

## ➡️ Next Step

Return to **Auth Flows** or explore **Plugin Domains**
