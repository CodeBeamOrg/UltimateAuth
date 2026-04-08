---
title: Credentials
order: 6
group: client
---


# 🔑 Credential Management Guide

This section explains how to manage user credentials (such as passwords) using the UltimateAuth client.

## 🧠 Overview

Credential operations are handled via:

```csharp
UAuthClient.Credentials...
```

This includes:

- password management
- credential reset flows
- admin credential operations

<br>

## 🔐 Change Password (Self)

```csharp
await UAuthClient.Credentials.ChangeMyAsync(new ChangeCredentialRequest
{
    CurrentSecret = "old-password",
    NewSecret = "new-password"
});
```

👉 Requires current password  
👉 Triggers `CredentialsChangedSelf` event

## 🔁 Reset Password (Self)

### Begin Reset

```csharp
var begin = await UAuthClient.Credentials.BeginResetMyAsync(
    new BeginResetCredentialRequest
    {
        Identifier = "user@mail.com"
    });
```

### Complete Reset

```csharp
await UAuthClient.Credentials.CompleteResetMyAsync(
    new CompleteResetCredentialRequest
    {
        Token = "reset-token",
        NewSecret = "new-password"
    });
```

👉 Typically used in:

- forgot password flows
- email-based reset flows

## ➕ Add Credential (Self)

```csharp
await UAuthClient.Credentials.AddMyAsync(new AddCredentialRequest
{
    Secret = "password"
});
```

## ❌ Revoke Credential (Self)

```csharp
await UAuthClient.Credentials.RevokeMyAsync(new RevokeCredentialRequest
{
    CredentialId = credentialId
});
```

👉 Useful for:

- removing login methods
- invalidating compromised credentials

<br>

## 👑 Admin: Change User Credential

```csharp
await UAuthClient.Credentials.ChangeUserAsync(userKey, new ChangeCredentialRequest
{
    NewSecret = "new-password"
});
```

👉 Does NOT require current password  

## 👑 Admin: Add Credential

```csharp
await UAuthClient.Credentials.AddUserAsync(userKey, new AddCredentialRequest
{
    Secret = "password"
});
```

## 👑 Admin: Revoke Credential

```csharp
await UAuthClient.Credentials.RevokeUserAsync(userKey, new RevokeCredentialRequest
{
    CredentialId = credentialId
});
```

## 👑 Admin: Reset Credential

### Begin

```csharp
await UAuthClient.Credentials.BeginResetUserAsync(userKey, request);
```

### Complete

```csharp
await UAuthClient.Credentials.CompleteResetUserAsync(userKey, request);
```

## ❌ Delete Credential (Admin)

```csharp
await UAuthClient.Credentials.DeleteUserAsync(userKey, new DeleteCredentialRequest
{
    CredentialId = credentialId
});
```

## 🧠 Credential Model

Credentials are:

- user-bound
- security-version aware
- lifecycle-managed

👉 Changing credentials may:

- invalidate sessions
- trigger security updates

<br>

## 🔐 Security Notes

- Passwords are never sent back to client
- All secrets are hashed server-side
- Reset flows should be protected (email, OTP, etc.)
- Admin operations are policy-protected

## 🎯 Summary

Credential management in UltimateAuth:

- supports self-service and admin flows  
- integrates with security lifecycle  
- enables safe credential rotation
