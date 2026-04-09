---
title: Authorization
order: 7
group: client
---


# 🛡 Authorization Guide

This section explains how to manage roles, permissions, and access control using the UltimateAuth client.

## 🧠 Overview

Authorization in UltimateAuth is policy-driven and role-based.

On the client, you interact with:
```
UAuthClient.Authorization
```

<br>

## 🔑 Core Concepts
Roles
- Named groups of permissions
- Assigned to users

Permissions
- Fine-grained access definitions
- Example: users.create.anonymous, users.delete.self, authorization.roles.admin

Policies
- Runtime decision rules
- Enforced automatically on server

<br>

### 📋 Query Roles
var result = await UAuthClient.Authorization.QueryRolesAsync(new RoleQuery
{
    PageNumber = 1,
    PageSize = 10
});

### ➕ Create Role
await UAuthClient.Authorization.CreateRoleAsync(new CreateRoleRequest
{
    Name = "Manager"
});

### ✏️ Rename Role
await UAuthClient.Authorization.RenameRoleAsync(new RenameRoleRequest
{
    Id = roleId,
    Name = "NewName"
});

### 🧩 Set Permissions
await UAuthClient.Authorization.SetRolePermissionsAsync(new SetRolePermissionsRequest
{
    RoleId = roleId,
    Permissions = new[]
    {
        Permission.From("users.read"),
        Permission.From("users.update")
    }
});

👉 Permissions support:

- Exact match → users.create
- Prefix → users.*
- Wildcard → *

### ❌ Delete Role
await UAuthClient.Authorization.DeleteRoleAsync(new DeleteRoleRequest
{
    Id = roleId
});

👉 Automatically removes role assignments from users

### 👤 Assign Role to User
await UAuthClient.Authorization.AssignRoleToUserAsync(new AssignRoleRequest
{
    UserKey = userKey,
    RoleName = "Manager"
});

### ➖ Remove Role
await UAuthClient.Authorization.RemoveRoleFromUserAsync(new RemoveRoleRequest
{
    UserKey = userKey,
    RoleName = "Manager"
});

### 📋 Get User Roles
var roles = await UAuthClient.Authorization.GetUserRolesAsync(userKey);
🔍 Check Authorization
var result = await UAuthClient.Authorization.CheckAsync(new AuthorizationCheckRequest
{
    Action = "users.delete"
});

👉 Returns:

- Allow / Deny
- Reason (if denied)

<br>

## 🧠 Permission Model

Permissions are normalized and optimized:

- Full access → *
- Group access → users.*
- Specific → users.create

👉 Internally compiled for fast evaluation

<br>

## 🔐 Security Notes
- Authorization is enforced server-side
- Client only requests actions
- Policies may override permissions
- Cross-tenant access is denied by default

## 🎯 Summary

Authorization in UltimateAuth:

- combines roles + permissions + policies
- is evaluated through a decision pipeline
- supports fine-grained and scalable access control
