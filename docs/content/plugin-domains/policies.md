# 🛡 Policies & Access Control

UltimateAuth uses a **policy-driven authorization model**.

Policies are not simple checks —  
they are **composable decision units** evaluated at runtime.

## 🧠 Mental Model

Authorization in UltimateAuth is:

👉 Context-based  
👉 Policy-driven  
👉 Orchestrated  

### Flow

1. Build `AccessContext`
2. Resolve policies
3. Execute authority
4. Allow / Deny / Reauth

<br>

## ⚙️ AccessContext

Every authorization decision is based on:

- Actor (who is calling)
- Target (what is being accessed)
- Action (what is being done)
- Tenant
- Claims / permissions

<br>

## 🔌 Policy Resolution

Policies are resolved using:

- Action prefix matching
- Runtime filtering (`AppliesTo`)

Example:

- `users.create.admin`
- `users.*`
- `authorization.roles.*`

<br>

## 🧩 Policy Types

### Global Policies

Always evaluated:

- RequireAuthenticated
- DenyCrossTenant

### Runtime Policies

Resolved dynamically:

- RequireActiveUser
- MustHavePermission
- RequireSelf

### Invariants

Executed first:

- Cannot be bypassed
- Hard security rules

<br>

## ⚖️ Policy Evaluation

Evaluation order:

1. Invariants
2. Global policies
3. Runtime policies

👉 First deny wins  
👉 Allow means “no objection”  
👉 Reauth can be requested

<br>

## 🔐 Example Policy

### Deny Admin Self Modification

- Blocks admin modifying own account
- Applies only to `.admin` actions
- Ignores read operations

### Require Active User

- Ensures user exists
- Ensures user is active
- Skips anonymous actions

<br>

## 🚀 Access Orchestrator

The orchestrator is the entry point:

- Enriches context (claims, permissions)
- Resolves policies
- Executes authority
- Runs command if allowed

## 🎯 Key Principles

- Policies are composable
- Authorization is deterministic
- No hidden magic
- Fully extensible

---

👉 Authorization is not a single check  
👉 It is a **pipeline of decisions**
