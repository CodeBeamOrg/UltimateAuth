---
title: Authentication Modes
order: 4
group: fundamentals
---


> Note: SemiHybrid and PureJwt modes will be available on future releases. For now you can safely use PureOpaque and Hybrid modes.

# 🔐 Authentication Modes

UltimateAuth supports multiple authentication modes.

Each mode represents a different balance between:

- Security  
- Performance  
- Control  
- Client capabilities  

👉 You don’t always choose a single model.  
UltimateAuth can adapt based on context.

<br>

## 🧩 Available Modes

### PureOpaque
Fully server-controlled session model.

### Hybrid
Combines session control with token-based access.

### SemiHybrid
JWT-based with server-side metadata awareness.

### PureJwt
Fully stateless token-based authentication.

<br>

## ⚖️ Mode Comparison

| Feature        | PureOpaque | Hybrid     | SemiHybrid     | PureJwt      |
|----------------|------------|------------|----------------|--------------|
| SessionId      | Required   | Required   | Metadata only  | None         |
| Access Token   | ❌         | ✔         | ✔              | ✔           |
| Refresh Token  | ❌         | ✔         | ✔              | ✔           |
| Revocation     | Immediate   | Immediate | Metadata-based | Not immediate|
| Statefulness   | Full        | Hybrid    | Semi           | Stateless    |
| Server Control | Full        | High      | Medium         | Low          |
| Performance*   | Medium      | Medium    | High           | Highest      |
| Offline Support| ❌         | Partial   | ✔              | ✔           |

> ⚡ **Performance Note**
>
> All modes in UltimateAuth are designed for production use and are highly optimized.
> 
> The differences here are about **trade-offs**, not absolute speed:
> 
> 👉 Even the most server-controlled mode is performant enough for real-world applications.

<br>

## 🧠 How to Think About Auth Modes
It’s important to understand that authentication modes in UltimateAuth are not rigid system-wide choices.

👉 You are not expected to pick a single mode and use it everywhere.

Instead:

- Different clients can use different modes on a single UAuthHub
- The mode can change **per request**  
- UltimateAuth selects the most appropriate mode based on **Client Profile and runtime context**

<br>

### 🔄 Runtime-Driven Behavior
In a typical application:

- Blazor Server → PureOpaque  
- Blazor WASM → Hybrid  
- API → PureJwt  

👉 All can coexist in the same system.

You don’t split your architecture — UltimateAuth adapts automatically.

### ⚙️ You Can Override Everything
UltimateAuth provides **safe defaults**, but nothing is locked.

You can:

- Force a specific auth mode  
- Customize behavior per client  
- Replace default strategies  

👉 The system is designed to be flexible without sacrificing safety.

### 🛡 Safe by Default
The comparison table shows trade-offs — not risks.

- All modes are **valid and supported**  
- Choosing a different mode will not “break” security  
- Incompatible configurations will **fail fast**  

👉 You are always operating within a safe boundary.

### 💡 Mental Model

Think of auth modes as:

> Different execution strategies — not different systems.

UltimateAuth remains consistent.  
Only the **behavior adapts**.

<br>

## 🔐 PureOpaque
- Fully session-based
- Every request validated on server
- Maximum security
- Touch semantics instead of refresh rotation

👉 Best for:

- Blazor Server
- Internal apps

## ⚡ Hybrid
- Access token as opaque session id
- Refresh token with rotate semantics
- Server control with API performance

👉 Best for:

- Blazor WASM
- Web + API systems
- Full-stack apps

## 🚀 SemiHybrid
- JWT-based access
- Server-side metadata control

👉 Best for:

- High-performance APIs
- Zero-trust systems

## 🌐 PureJwt
- Fully stateless
- No server-side session control

👉 Best for:

- External APIs
- Microservices

## 🎯 Which Mode Should You Use?

| Scenario               | Recommended Mode |
|------------------------|------------------|
| Blazor Server          | PureOpaque       |
| Web + API              | Hybrid           |
| High-scale API         | SemiHybrid       |
| External microservices | PureJwt          |
