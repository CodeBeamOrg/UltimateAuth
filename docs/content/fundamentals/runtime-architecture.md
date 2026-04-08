---
title: Runtime Architecture
order: 6
group: fundamentals
---


# 🏗 Runtime Architecture

UltimateAuth processes authentication through a structured execution pipeline.

👉 It is not just middleware-based authentication  
👉 It is a **flow-driven execution system**

## 🧠 The Big Picture

When a request reaches an auth endpoint:

```text
Request
  → Endpoint Filter
  → AuthFlowContext
  → Endpoint Handler
  → Flow Service
  → Orchestrator
  → Authority
  → Stores / Issuers
  → Response
```
Each step has a clearly defined responsibility.

## 🔄 Request Entry Point
Authentication begins at the endpoint level.

An endpoint filter detects the flow:

`Endpoint → FlowType (Login, Refresh, Logout…)`

👉 The system knows which flow is being executed before any logic runs.

<br>

## 🧾 AuthFlowContext — The Core State
Before any operation starts, UltimateAuth creates an AuthFlowContext.

This is the central object that carries:

- Client profile
- Effective authentication mode
- Tenant information
- Device context
- Session state
- Response configuration

👉 This context defines the entire execution environment

<br>

## ⚙️ Flow Service — Entry Layer
After the context is created, the request is passed to the Flow Service.

The Flow Service:

- Acts as the entry point for all flows
- Normalizes execution
- Delegates work to orchestrators

👉 It does not implement business logic directly

<br>

## 🧭 Orchestrator — Flow Coordinator
The Orchestrator manages the execution of a flow.

- Coordinates multiple steps
- Ensures correct execution order
- Delegates decisions to Authority

👉 Think of it as the flow engine

<br>

## 🔐 Authority — Decision Layer
The Authority is the most critical component.

- Validates authentication state
- Applies security rules
- Approves or rejects operations

👉 No sensitive operation bypasses Authority

<br>

## ⚙️ Services & Stores
Once decisions are made:

- Services handle domain logic
- Stores handle persistence
- Issuers generate tokens or session artifacts

👉 These layers do not make security decisions

<br>

## 🔁 End-to-End Example (Login)
Login Request
```
  → Endpoint Filter (Login Flow)
  → AuthFlowContext created
  → Flow Service
  → Orchestrator
  → Authority validates credentials
  → Session created (Store)
  → Tokens issued (Issuer)
  → Response generated
```
<br>

## 🧠 Why This Architecture Matters
✔ Centralized Decision Making
- Authority is always in control
- No scattered validation logic

✔ Predictable Execution
- Every flow follows the same pipeline
- No hidden behavior

✔ Extensibility
- Replace stores
- Extend flows
- Customize orchestration

✔ Security by Design
- No bypass of Authority
- Context-driven validation
- Flow-aware execution

## 🔗 Relation to Other Concepts
This architecture connects all previous concepts:

- Auth Model (Root / Chain / Session) → validated in Authority
- Auth Flows → executed by Orchestrator
- Auth Modes → applied via EffectiveMode
- Client Profiles → influence behavior at runtime

## 🧠 Mental Model

If you remember one thing:

👉 Flow defines what happens
👉 Context defines how it happens
👉 Authority decides if it happens

## 📌 Key Takeaways
Authentication is executed as a pipeline
AuthFlowContext carries execution state
Orchestrator coordinates flows
Authority enforces security
Services and Stores execute operations

## ➡️ Next Step

Now that you understand the execution model:

👉 Continue to Request Lifecycle
