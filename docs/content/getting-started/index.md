---
title: Getting Started
order: 1
group: getting-started
---

# 🚀 Getting Started

Welcome to **UltimateAuth** — the modern authentication framework for .NET.

UltimateAuth is designed to make authentication **simple to use**, while still being **powerful, flexible, and deeply secure** enough for real-world applications.

## What is UltimateAuth?

UltimateAuth is a **flow-driven authentication framework** that reimagines how authentication works in modern .NET applications.

It unifies:

- Session-based authentication
- Token-based authentication (JWT)
- PKCE flows for public clients
- Multi-client environments (Blazor Server, WASM, MAUI, APIs)

into a single, consistent system.

Instead of choosing between cookies, sessions, or tokens, UltimateAuth allows you to use **the right model for each scenario — automatically**.

## What Makes UltimateAuth Different?

### 🔹 Session is a First-Class Concept

Unlike traditional systems, UltimateAuth treats sessions as a **core domain**, not a side effect.

- Root → global security authority  
- Chain → device context  
- Session → actual authentication instance  

This allows:

- Instant revocation  
- Multi-device control  
- Secure session lifecycle management

### 🔹 Flow-Based, Not Token-Based

UltimateAuth is not cookie-based or token-based.

It is **flow-based**:

- Login is a flow  
- Refresh is a flow  
- Re-authentication is a flow  

👉 Authentication becomes **explicit, predictable, and controllable**

### 🔹 Built for Blazor and Modern Clients

UltimateAuth is designed from the ground up for:

- Blazor Server  
- Blazor WASM  
- .NET MAUI  

With:

- Native PKCE support  
- Built-in Blazor components (`UAuthLoginForm`, `UAuthApp`)  
- Automatic client profile detection  

👉 No hacks. No manual glue code.

### 🔹 Runtime-Aware Authentication

Authentication behavior is not static.

UltimateAuth adapts based on:

- Client type  
- Auth mode  
- Request context  

👉 Same system, different optimized behavior.

## What Problems It Solves

UltimateAuth addresses real-world challenges:

### 🔹 Multiple Client Types

Blazor Server, WASM, MAUI, and APIs all behave differently.  

UltimateAuth handles these differences automatically using **Client Profiles**.

### 🔹 Session vs Token Confusion

Should you use cookies, sessions, or JWT?

UltimateAuth removes this decision by supporting multiple auth modes and selecting the correct behavior at runtime.

### 🔹 Secure Session Management
- Device-aware sessions  
- Session revocation  
- Refresh token rotation  
- Replay protection  

All built-in — no custom implementation required.

### 🔹 Complex Auth Flows
Login, logout, refresh, PKCE, multi-device control etc.

All exposed as **simple application-level APIs**.

## When to Use UltimateAuth
Use UltimateAuth if:

- You are building a modern .NET application **Blazor Server, WASM or MAUI**
- You need **session + token hybrid authentication**
- You want **full control over authentication flows**
- You are building a **multi-client system (web + mobile + API)**
- You need **strong security with extensibility**


👉 Continue to **Quick Start** to build your first UltimateAuth application.

