---
title: Identifiers
order: 5
group: client
---


# 🆔 User Identifiers Guide

This section explains how UltimateAuth manages user identifiers such as email, username, and phone.

## 🧠 Overview

In UltimateAuth, identifiers are **first-class entities**.

They are NOT just fields on the user.

👉 On the client, you interact with:

```csharp
UAuthClient.Identifiers...
```

<br>

## 🔑 What is an Identifier?

An identifier represents a way to identify a user:

- Email
- Username
- Phone number
- Custom identifiers

👉 Each identifier has:

- Value (e.g. user@ultimateauth.com)
- Type (email, username, etc.)
- Verification state
- Primary flag

<br>

## ⭐ Primary Identifier

A user can have multiple identifiers, but only one can be **primary**. Setting an identifier to primary automatically unset the current primary identifier if exists.

```csharp
await UAuthClient.Identifiers.SetMyPrimaryAsync(new SetPrimaryUserIdentifierRequest
{
    Id = identifierId
});
```

👉 Primary identifier is typically:

- Used for display
- Preferred for communication


## 🔐 Login Identifiers

Not all identifiers are used for login.

👉 **Login identifiers are a subset of identifiers**

👉 This is configurable:

- Enable/disable per type
- Custom logic can be applied

<br>

## 📋 Get My Identifiers

```csharp
var result = await UAuthClient.Identifiers.GetMyAsync();
```


## ➕ Add Identifier

```csharp
await UAuthClient.Identifiers.AddMyAsync(new AddUserIdentifierRequest
{
    Identifier = "new@ultimateauth.com",
    Type = UserIdentifierType.Email,
    IsPrimary = true
});
```

<br>

## ✏️ Update Identifier

```csharp
UpdateUserIdentifierRequest updateRequest = new()
{
    Id = item.Id,
    NewValue = item.Value
};

await UAuthClient.Identifiers.UpdateMyAsync(updateRequest);
```


## ✅ Verify Identifier

```csharp
await UAuthClient.Identifiers.VerifyMyAsync(new VerifyUserIdentifierRequest
{
    Id = identifierId
});
```


## ❌ Delete Identifier

```csharp
await UAuthClient.Identifiers.DeleteMyAsync(new DeleteUserIdentifierRequest
{
    Id = identifierId
});
```

<br>

## 👤 Admin Operations

### Get User Identifiers

```csharp
await UAuthClient.Identifiers.GetUserAsync(userKey);
```


### Add Identifier to User

```csharp
await UAuthClient.Identifiers.AddUserAsync(userKey, request);
```


### Update Identifier

```csharp
await UAuthClient.Identifiers.UpdateUserAsync(userKey, request);
```


### Delete Identifier

```csharp
await UAuthClient.Identifiers.DeleteUserAsync(userKey, request);
```


## 🔄 State Events

Identifier changes trigger events:

- IdentifiersChanged

👉 Useful for:

- UI updates
- Cache invalidation

<br>

## 🔐 Security Considerations

- Identifiers may require verification
- Login identifiers can be restricted
- Primary identifier can be controlled


## 🎯 Summary

UltimateAuth identifiers:

- are independent entities
- support multiple types
- separate login vs non-login identifiers
- are fully manageable via client
