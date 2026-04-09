---
title: Authentication
order: 2
group: client
---


# 🔐 Authentication Guide

This section explains how to use the UltimateAuth client for authentication flows.

## 🧠 Overview

Authentication in UltimateAuth is **flow-based**, not endpoint-based.

You interact with:

👉 `FlowClient`

---

## 🔑 Login

### Basic Login

```csharp
await UAuthClient.Flows.LoginAsync(new LoginRequest
{
    Identifier = "user@ultimateauth.com",
    Secret = "password"
});
```

👉 This triggers a full login flow:

- Sends credentials
- Handles redirect
- Establishes session

---

## ⚡ Try Login (Pre-check)

```csharp
var result = await UAuthClient.Flows.TryLoginAsync(
    new LoginRequest
    {
        Identifier = "user@mail.com",
        Secret = "password"
    },
    UAuthSubmitMode.TryOnly
);
```

### Modes

| Mode         | Behavior                       |
|--------------|--------------------------------|
| TryOnly      | Validate only                  |
| DirectCommit | Skip validation                |
| TryAndCommit | Validate then login if success |

👉 Use `DirectCommit` when:
- You need maximum performance while sacrificing interactive SPA capabilities.

👉 Use `TryOnly` when:

- You need validation feedback
- You want custom UI flows

👉 Use `TryAndCommit` when:

- You need completely interactive SPA experience.

👉 `TryAndCommit` is the recommended mode for most applications.

It provides:

- Validation feedback
- Automatic redirect on success
- Smooth SPA experience

<br>

## 🔄 Refresh

```csharp
var result = await UAuthClient.Flows.RefreshAsync();
```

### Possible Outcomes

- Success → new tokens/session updated  
- Touched → session extended  
- Rotated → refresh token rotated  
- NoOp → nothing changed  
- ReauthRequired → login required

👉 Refresh behavior depends on auth mode:

- PureOpaque → session touch
- Hybrid/JWT → token rotation

In default, successful refresh returns success outcome. If you want to learn success detail such as no-op, touched or rotated, open it via server options:

```csharp
builder.Services.AddUltimateAuthServer(o =>
{
    o.Diagnostics.EnableRefreshDetails = true;
});
```

<br>

## 🚪 Logout

```csharp
await UAuthClient.Flows.LogoutAsync();
```

👉 This:

- Ends current session
- Clears authentication state

<br>

## 📱 Device Logout Variants

```csharp
await UAuthClient.Flows.LogoutMyDeviceAsync(sessionId);
await UAuthClient.Flows.LogoutMyOtherDevicesAsync();
await UAuthClient.Flows.LogoutAllMyDevicesAsync();
```

👉 These operate on **session chains (devices)**

<br>

## 🔐 PKCE Flow (Public Clients)

### Start PKCE

```csharp
await UAuthClient.Flows.BeginPkceAsync();
```

### Complete PKCE

```csharp
await UAuthClient.Flows.CompletePkceLoginAsync(request);
```

> Complete PKCE also has try semantics the same as login flow. UltimateAuth suggests to use `TryCompletePkceLoginAsync` for complete interactive experience.

👉 Required for:

- SPA
- Blazor WASM
- Mobile apps

---

## 🚨 Security Note

- Public clients MUST use PKCE
- Server clients MAY allow direct login

Direct credential posting disabled by default and throws exception when you directly call login. You can enable it via options. You should only use it for debugging and development purposes.
```csharp
builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.Login.AllowCredentialPost = true;
});
---

## 🎯 Summary

Authentication in UltimateAuth:

- is flow-driven
- adapts to client type
- enforces security by design

---

👉 Always use `FlowClient` for authentication operations
