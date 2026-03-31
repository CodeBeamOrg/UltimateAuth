# 🧠 Configuration Overview

UltimateAuth is not configured as a static system.

👉 It is configured as a **runtime-adaptive system**

## ⚠️ A Common Misunderstanding

Many frameworks expect you to configure authentication once:

- Choose JWT or cookies  
- Set token lifetimes  
- Configure behavior globally

👉 And that configuration applies everywhere

👉 UltimateAuth does NOT work like this

<br>

## 🧩 Layered Configuration Model

UltimateAuth separates configuration into distinct layers:

### 🔹 Core (Behavior Definition)

Core defines **what authentication means**:

- Session lifecycle  
- Token behavior  
- PKCE rules  
- Multi-tenant handling  

👉 Core is the foundation

👉 But you typically do NOT configure it directly

### 🔹 Server (Application Configuration)

Server is where you configure the system:

```csharp
builder.Services.AddUltimateAuthServer(o =>
{
    o.Login.MaxFailedAttempts = 5;
});
```

This layer controls:

- Allowed authentication modes  
- Endpoint exposure  
- Cookie behavior  
- Security policies  

👉 This is your main configuration surface

### 🔹 Client (Runtime Behavior)

Client configuration controls:

- Client profile (WASM, Server, MAUI, API)  
- PKCE behavior  
- Auto-refresh  
- Re-authentication  

👉 Client influences how flows are executed

<br>

## ⚡ Runtime Configuration (The Key Idea)

Here is the most important concept:

👉 UltimateAuth does NOT use configuration as-is

Instead, it computes **Effective Configuration per request**

### 🧠 How It Works

At runtime:

1. Client profile is detected  
2. Flow type is determined (Login, Refresh, etc.)  
3. Auth mode is resolved  
4. Defaults are applied  
5. Mode-specific overrides are applied  

👉 This produces:

```
EffectiveOptions
```

<br>

## 🔄 From Static to Dynamic

```
UAuthServerOptions (startup)
        ↓
Mode Resolver
        ↓
Apply Defaults
        ↓
Mode Overrides
        ↓
EffectiveUAuthServerOptions (runtime)
```

👉 Every request can have a different effective configuration

<br>

## 🎯 Why This Matters

This allows UltimateAuth to:

- Use different auth modes per client  
- Adapt behavior per flow  
- Enforce security dynamically  
- Avoid global misconfiguration  

👉 You don’t configure “one system”

👉 You configure a **decision engine**

<br>

## 🛡 Safety by Design

Even with dynamic behavior:

- Invalid combinations fail early  
- Disallowed modes are rejected  
- Security invariants are enforced  

👉 Flexibility does not reduce safety

<br>

## ⚙️ Core vs Effective

| Concept            | Meaning                        |
|-------------------|---------------------------------|
| Core Options      | Base behavior definitions       |
| Server Options    | Application-level configuration |
| Effective Options | Runtime-resolved behavior       |

👉 Effective options are what actually run

<br>

## 🧠 Mental Model

If you remember one thing:

👉 You don’t configure authentication  

👉 You configure how it is **resolved**

## ➡️ Next Step

- Deep dive into behavior → Core Options  
- Control runtime → Server Options  
- Configure clients → Client Options  
