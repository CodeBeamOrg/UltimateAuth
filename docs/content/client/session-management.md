---
title: Session Management
order: 3
group: client
---


# 📱 Session Management Guide

This section explains how to manage sessions and devices using the UltimateAuth client.

## 🧠 Overview

In UltimateAuth, sessions are **not just tokens**.

They are structured as:

- Root → user-level security
- Chain → device (browser / mobile / app)
- Session → individual authentication instance

👉 On the client, you interact with:

```csharp
[Inject] IUAuthClient UAuthClient { get; set; } = null!;

await UAuthClient.Sessions...
```

## 📋 Get Active Sessions (Devices)

```csharp
var result = await UAuthClient.Sessions.GetMyChainsAsync();
```

👉 Returns:

- Active devices
- Session chains
- Metadata (device, timestamps)

<br>

## 🔍 Get Session Detail

```csharp
var detail = await UAuthClient.Sessions.GetMyChainDetailAsync(chainId);
```

👉 Use this to:

- Inspect a specific device
- View session history

<br>

## 🚪 Logout vs Revoke (Important)

UltimateAuth distinguishes between:

### Logout

```csharp
await UAuthClient.Flows.LogoutAsync();
```

- Ends **current session**
- User can login again normally
- Does NOT affect other devices

### Revoke (Session Control)

```csharp
await UAuthClient.Sessions.RevokeMyChainAsync(chainId);
```

- Invalidates entire **device chain**
- All sessions under that device are revoked
- Cannot be restored
- New login creates a new chain

👉 Key difference:

- Logout = end current session
- Revoke = destroy device identity

For standard auth process, use `UAuthClient.Flows.LogoutMyDeviceAsync(chainId)` instead of `UAuthClient.Sessions.RevokeMyChainAsync(chainId)`

<br>

## 📱 Revoke Other Devices

```csharp
await UAuthClient.Sessions.RevokeMyOtherChainsAsync();
```

👉 This:

- Keeps current device active
- Logs out all other devices

<br>

## 💥 Revoke All Sessions

```csharp
await UAuthClient.Sessions.RevokeAllMyChainsAsync();
```

👉 This:

- Logs out ALL devices (including current)
- Forces full reauthentication everywhere

<br>

## 👤 Admin Session Management

### Get User Devices

```csharp
await UAuthClient.Sessions.GetUserChainsAsync(userKey);
```

### Revoke Specific Session

```csharp
await UAuthClient.Sessions.RevokeUserSessionAsync(userKey, sessionId);
```

### Revoke Device (Chain)

```csharp
await UAuthClient.Sessions.RevokeUserChainAsync(userKey, chainId);
```

### Revoke All User Sessions

```csharp
await UAuthClient.Sessions.RevokeAllUserChainsAsync(userKey);
```

<br>

## 🧠 Device Model

Each chain represents a **device identity**:

- Browser instance
- Mobile device
- Application instance

👉 Sessions are grouped under chains.

<br>

## 🔐 Security Implications

Session operations are security-critical:

- Revoke is irreversible
- Device isolation is enforced
- Cross-device attacks are contained

## 🎯 Summary

Session management in UltimateAuth:

- is device-aware
- separates logout vs revoke
- gives full control over user sessions
