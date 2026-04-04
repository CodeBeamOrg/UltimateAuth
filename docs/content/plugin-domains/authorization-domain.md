# 🔐 Authorization & Policies

UltimateAuth provides a flexible and extensible authorization system based on:

- Roles
- Permissions
- Policies
- Access orchestration

## 🧩 Core Concepts

### 🔑 Permissions

In UltimateAuth, permissions are not just arbitrary strings.

They follow a **structured action model**.

#### 🧩 Permission Structure

Permissions are built using a consistent format:

```
resource.operation.scope
```
or
```
resource.subresource.operation.scope
```

#### ✅ Examples

- `users.create.admin`
- `users.profile.update.self`
- `sessions.revokechain.admin`
- `credentials.change.self`

👉 This structure is not accidental —  
it is **designed for consistency, readability, and policy evaluation**.

---

### ⚙️ Built-in Action Catalog

UltimateAuth provides a predefined action catalog.

Examples:

- `flows.logout.self`
- `sessions.listchains.admin`
- `users.delete.self`
- `credentials.revoke.admin`
- `authorization.roles.assign.admin`

👉 This ensures:

- No magic strings
- Discoverable permissions
- Consistent naming across the system

<br>

### 🧠 Scope Semantics

The last part of the permission defines **scope**:

| Scope      | Meaning                          |
|------------|----------------------------------|
| `self`     | User acts on own resources       |
| `admin`    | User acts on other users         |
| `anonymous`| No authentication required       |

<br>

### 🌲 Wildcards & Grouping

Permissions support hierarchical matching:

- `users.*` → all user actions  
- `users.profile.*` → all profile operations  
- `*` → full access  

### ⚡ Normalization

Permissions are automatically normalized:

- Full coverage → replaced with `*`
- Full group → replaced with `prefix.*`

<br>

### Role

A role is a collection of permissions.

- Roles are tenant-scoped
- Roles can be dynamically updated
- Permissions are normalized internally

### UserRole

Users are assigned roles:

- Many-to-many relationship
- Assignment is timestamped
- Role resolution is runtime-based

<br>

## 🔄 Permission Resolution

Permissions are evaluated using:

- Exact match
- Prefix match
- Wildcard match

CompiledPermissionSet optimizes runtime checks.

<br>

## 🧠 Claims Integration

Authorization integrates with authentication via claims:

- Roles → `ClaimTypes.Role`
- Permissions → `permission` claim
- Tenant → `tenant`

This allows:

- Token-based authorization
- Stateless permission checks (for JWT modes)

<br>

## ⚙️ Authorization Flow

Authorization is executed through:

👉 AccessOrchestrator

Steps:

1. Build AccessContext
2. Execute policies
3. Allow or deny operation

<br>

## 🛡 Policies

Policies are the core of authorization logic.

Default policies include:

- RequireAuthenticated
- DenyCrossTenant
- RequireActiveUser
- RequireSelf
- RequireSystem
- MustHavePermission

<br>

## 🔌 Plugin Integration

Authorization is a plugin domain.

It:

- Does NOT depend on other domains
- Uses contracts only
- Integrates via policies and claims

## 🎯 Key Takeaways

- Authorization is policy-driven
- Roles are permission containers
- Permissions support wildcard & prefix
- Policies enforce rules
- Fully extensible and replaceable
