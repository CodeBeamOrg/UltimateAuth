---
title: User Management
order: 4
group: client
---


# 👤 User Management Guide

This section explains how to manage users using the UltimateAuth client.

## 🧠 Overview

User operations are handled via:

```csharp
UAuthClient.Users...
```

This includes:

- Profile management
- User lifecycle
- Admin operations

<br>

## 🙋 Get Current User

```csharp
var me = await UAuthClient.Users.GetMeAsync();
```

👉 Returns:

- User profile
- Status
- Basic identity data

<br>

## ✏️ Update Profile

```csharp
await UAuthClient.Users.UpdateMeAsync(new UpdateProfileRequest
{
    DisplayName = "John Doe"
});
```

👉 Triggers:

- Profile update
- State event (ProfileChanged)

<br>

## ❌ Delete Current User

```csharp
await UAuthClient.Users.DeleteMeAsync();
```

👉 This:

- Deletes user (based on configured mode)
- Ends session
- Triggers state update

<br>

## 👑 Admin: Query Users

```csharp
var result = await UAuthClient.Users.QueryAsync(new UserQuery
{
    Search = "john",
    PageNumber = 1,
    PageSize = 10
});
```

👉 Supports:

- search
- pagination
- filtering (status, etc.)

<br>

## ➕ Create User

```csharp
await UAuthClient.Users.CreateAsync(new CreateUserRequest
{
    UserName = "john",
    Email = "john@mail.com",
    Password = "123456"
});
```

## 🛠 Admin Create

```csharp
await UAuthClient.Users.CreateAsAdminAsync(request);
```

<br>

## 🔄 Change Status

### Self

```csharp
await UAuthClient.Users.ChangeMyStatusAsync(new ChangeUserStatusSelfRequest
{
    Status = UserStatus.SelfSuspended
});
```

### Admin

```csharp
await UAuthClient.Users.ChangeUserStatusAsync(userKey, new ChangeUserStatusAdminRequest
{
    Status = UserStatus.Suspended
});
```

<br>

## ❌ Delete User (Admin)

```csharp
await UAuthClient.Users.DeleteUserAsync(userKey, new DeleteUserRequest
{
    Mode = DeleteMode.Soft
});
```

## 🔍 Get User

```csharp
var user = await UAuthClient.Users.GetUserAsync(userKey);
```

## ✏️ Update User (Admin)

```csharp
await UAuthClient.Users.UpdateUserAsync(userKey, request);
```

## 🧠 Lifecycle Model

Users have a lifecycle:

- Active
- Suspended
- Disabled
- Deleted (soft/hard)

👉 Status impacts:

- login ability
- session validity
- authorization

<br>

## 🔐 Security Notes

- Status changes may invalidate sessions
- Delete may trigger cleanup across domains
- Admin actions are policy-protected

## 🎯 Summary

User management in UltimateAuth:

- is lifecycle-aware
- supports self + admin flows
- integrates with session & security model
