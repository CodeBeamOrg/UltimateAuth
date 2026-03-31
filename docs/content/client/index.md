# 🚀 Client Usage Guide

UltimateAuth Client is a **high-level SDK** designed to simplify authentication flows.

It is NOT just an HTTP wrapper.

## 🧠 What Makes the Client Different?

The client:

- Handles full authentication flows (login, PKCE, refresh)
- Manages redirects automatically
- Publishes state events
- Provides structured results
- Works across multiple client types (SPA, server, hybrid)

👉 You SHOULD use the client instead of calling endpoints manually.

<br>

## 🧱 Client Architecture

The client is split into multiple specialized services:

| Service              | Responsibility                      |
|----------------------|-------------------------------------|
| FlowClient           | Login, logout, refresh, PKCE        |
| SessionClient        | Session & device management         |
| UserClient           | User profile & lifecycle            |
| IdentifierClient     | Email / username / phone management |
| CredentialClient     | Password management                 |
| AuthorizationClient  | Roles & permissions                 |

<br>

## 🔑 Core Concept: Flow-Based Design

UltimateAuth is **flow-oriented**, not endpoint-oriented.

Instead of calling endpoints:

❌ POST /auth/login  
✔ flowClient.LoginAsync()

<br>

## ⚡ Example

```csharp
await flowClient.LoginAsync(new LoginRequest
{
    Identifier = "user@ultimateauth.com",
    Secret = "password"
});
```

👉 This automatically:

- Builds request payload
- Handles redirect
- Integrates with configured endpoints

<br>

## 🔄 State Events

Client automatically publishes events:

- SessionRevoked
- ProfileChanged
- AuthorizationChanged

<br>

## 🧭 How to Use This Section

Follow these guides:

- authentication.md → login, refresh, logout
- session-management.md → sessions & devices
- user-management.md → user operations
- identifiers.md → login identifiers
- authorization.md → roles & permissions

## 🎯 Summary

UltimateAuth Client:

- abstracts complexity
- enforces correct flows
- reduces security mistakes


👉 Think of it as:

**“Authentication runtime for your frontend / app”**
