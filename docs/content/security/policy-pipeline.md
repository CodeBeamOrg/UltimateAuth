---
title: Policy Pipeline
order: 4
group: security
---


# 🧠 Policy Pipeline Deep Dive

UltimateAuth does not rely on simple role checks.

Authorization is executed through a **multi-stage policy pipeline** that evaluates:

- invariants (always enforced rules)
- global policies
- runtime (action-based) policies

## 🧠 Core Principle

👉 Authorization is not a single check  
👉 It is a **decision pipeline**

## 🔁 High-Level Flow

```text
AccessContext
   ↓
Enrichment (claims, permissions)
   ↓
Invariants
   ↓
Global Policies
   ↓
Runtime Policies (action-based)
   ↓
Final Decision
```

## 🧩 AccessContext

Every authorization decision is based on an `AccessContext`.

It contains:

- actor (who is making the request)
- target (resource / user)
- tenant
- action (string-based, e.g. "users.delete")
- attributes (claims, permissions, etc.)

👉 Everything in the pipeline reads from this context

## 🔄 Step 1: Context Enrichment

Before policies run, context is enriched:

- permissions are loaded
- compiled into `CompiledPermissionSet`
- attached to context

👉 This allows policies to run without hitting storage repeatedly

## 🛡 Step 2: Invariants

Invariants are **always enforced rules**.

They run first and cannot be bypassed.

Examples:

- user must be authenticated
- cross-tenant access is denied
- request must be valid

👉 If an invariant fails:

```text
→ DENY immediately
```

## 🌐 Step 3: Global Policies

Global policies apply to all requests but are conditional.

Examples:

- RequireActiveUserPolicy
- DenyAdminSelfModificationPolicy

Each policy:

- decides if it applies (`AppliesTo`)
- evaluates (`Decide`)

👉 Important:

- “Allow” means *no objection*
- not final approval

## 🎯 Step 4: Runtime Policies

Runtime policies are:

- action-based
- dynamically resolved

They come from:

```text
AccessPolicyRegistry → CompiledAccessPolicySet
```

Policies are selected by:

```text
context.Action.StartsWith(prefix)
```

Example:

```text
Action: users.delete.admin

Matches:
- users.*
- users.delete.*
- users.delete.admin
```

## ⚖️ Step 5: Decision Engine

All policies are evaluated by:

👉 `IAccessAuthority`

Evaluation order:

1. Invariants  
2. Global policies  
3. Runtime policies

### Decision Rules

- First **deny** → stops execution  
- “Allow” → continue  
- “RequiresReauthentication” → tracked

Final result:

```text
Allow
Deny(reason)
ReauthenticationRequired
```

## 🔐 Permission Integration

Permissions are not directly checked in services.

Instead:

- permissions are compiled
- policies use them

Example policy:

```text
MustHavePermissionPolicy
```

Checks:

```text
CompiledPermissionSet.IsAllowed(action)
```


👉 This decouples:

- permission storage
- authorization logic

## 🔥 Why This Matters

This pipeline enables:

- composable security rules
- consistent enforcement
- separation of concerns
- extensibility

Compared to typical systems:

| Feature              | Traditional | UltimateAuth |
|---------------------|------------|-------------|
| Inline checks       | ✅         | ❌          |
| Central pipeline    | ❌         | ✅          |
| Policy composition  | ❌         | ✅          |
| Action-based rules  | ❌         | ✅          |

## ⚠️ Design Tradeoff

This model introduces:

- more abstraction
- more components

👉 But gives:

- predictability
- auditability
- flexibility

## 🧠 Mental Model

If you remember one thing:

👉 Authorization is not “if (role == admin)”  
👉 It is a **pipeline of decisions**

## ➡️ Summary

UltimateAuth authorization:

- is policy-driven  
- runs through a structured pipeline  
- separates invariants, global rules, and action rules  
- produces deterministic decisions

👉 This makes it suitable for complex, multi-tenant, security-sensitive systems
