---
title: Getting Started
order: 1
group: getting-started
---

# 🚀 Getting Started

Welcome to **UltimateAuth**, a unified authentication and authorization framework for modern .NET applications.

Authentication should not become a different system every time your application gains a new client, tenant, device, or security requirement. UltimateAuth brings these concerns into **one coherent model**—without pretending that every application needs the same authentication strategy.

**One framework. Multiple authentication models. A consistent security lifecycle.**

## What is UltimateAuth?

UltimateAuth is a modular, flow-driven framework for building and managing authentication in .NET. It connects concepts that are often implemented separately:

- **Sessions, cookies and tokens** — stateful session management alongside access and refresh token workflows.
- **Authentication and authorization** — identity establishment and access decisions within a consistent architecture.
- **Users and credentials** — identity, identifiers, credentials, and their security state.
- **Devices and session continuity** — device-aware session chains, refresh, logout, and revocation.
- **Clients and delivery mechanisms** — browser, API, Blazor, and mobile scenarios with client-aware behavior.
- **Tenants and isolation** — tenant-aware request context and authentication boundaries.
- **Security policies and execution flows** — explicit decisions, validation, and controlled state transitions.

The goal is not to hide these concepts. It is to make them **work together predictably**.

## Unified Does Not Mean One-Size-Fits-All

A server-rendered web application, a browser-based SPA, a mobile app, and a backend API do not share the same trust boundaries or credential transport requirements.

UltimateAuth provides a common foundation while allowing behavior to be shaped by the **effective authentication mode**, **client profile**, **tenant**, and **request context**.

| Concern | What UltimateAuth brings together | Why it matters |
| --- | --- | --- |
| Authentication models | Session-oriented and token-oriented workflows | Different clients can participate in one security architecture |
| Session lifecycle | Root → Chain → Session | Manage authentication at identity, device, and session scope |
| Client environments | ASP.NET Core, Blazor, APIs, and .NET mobile clients | Avoid rebuilding the core authentication model for each frontend |
| Multi-tenancy | Tenant resolution and tenant-bound context | Keep authentication operations within the intended tenant boundary |
| Credentials and identities | User identifiers, credential providers, and security state | Separate *who the user is* from *how they prove it* |
| Security enforcement | Context, validation, authority, and orchestration | Make decisions explicit rather than scattering checks across endpoints |
| Credential delivery | Mode- and client-aware response policies | Handle cookies, tokens, and redirects according to the scenario |
| Session termination | Logout, targeted revocation, and device management | Respond proportionately to ordinary sign-out and security incidents |

**Unified** is therefore about a shared security model—not forcing all applications to use identical cookies, tokens, or endpoints.

## Authentication Is More Than Issuing a Token

In many applications, authentication is described as a short sequence: validate credentials, generate a token, return it to the client.

That is only part of the story.

A production authentication system must also answer questions such as:

- Which tenant does this request belong to?
- Which user and credential are being evaluated?
- Is an existing session still valid?
- Is the request coming from a known device?
- Can access be renewed, restricted, or terminated?
- What should happen when a credential changes or a device is no longer trusted?

UltimateAuth treats authentication as a **controlled lifecycle**. Login, refresh, and logout are operations within that lifecycle—not unrelated endpoints that happen to exchange tokens.

## Sessions Are First-Class

Where session-backed modes apply, UltimateAuth models authentication through a hierarchy:

```text
User
 └── Root        Global session-security boundary
      └── Chain  Device-associated lifecycle
           └──   Session  Individual authentication instance
```

This structure gives the system a vocabulary for different security actions. A session can be invalidated, a device-associated chain can be terminated, and a broader security boundary can be revoked.

**A token is not the entire authentication model.** It is a credential used within that model, subject to the applicable mode and validation rules.

Not every authentication mode has identical persistence or session behavior. The session hierarchy describes the stateful lifecycle where that model is in use.

## Flow-Driven by Design

UltimateAuth organizes authentication operations around explicit flows. A flow brings together the request context, operation-specific validation, security decisions, permitted state transitions, and response behavior.

```text
Request
  → Tenant, session, and user resolution
  → AuthFlowContext
  → Flow-specific validation
  → Security decision and orchestration
  → State transition (when permitted)
  → Result and client-appropriate response
```

This is not a claim that pipelines or orchestration are new inventions. The value is in applying them **consistently across authentication operations**, so the rules are easier to reason about, test, and extend.

For the implementation model, see [Auth Flows](/docs/auth-flows/index).

## Tenant-Aware from the Request Boundary

Multi-tenancy is not simply a `TenantId` added to a user table. Authentication must understand *which tenant's security context* a request is operating within.

UltimateAuth resolves tenant context as part of the ASP.NET Core request pipeline and carries it into the authentication flow. The flow context then combines that boundary with client settings, device information, and applicable session validation.

This helps teams design multi-tenant applications without treating tenant isolation as an afterthought. **Tenant resolution and configuration still need to be correctly integrated by the host application.**

## Built for Different Clients, Not Just Different Screens

UltimateAuth is designed for applications that may span several environments:

- **ASP.NET Core web applications** with server-managed authentication concerns.
- **Blazor Server and Blazor WebAssembly** with different hosting and transport constraints.
- **.NET MAUI** and other native clients with device-oriented authentication needs.
- **HTTP APIs** serving browsers, applications, and other services.

Client profiles and effective server options help adapt authentication behavior to the requesting client. Response policies determine how applicable credentials are delivered and whether a redirect belongs in the interaction.

The result is a common backend security vocabulary across multiple application types—not a promise that every client uses the same transport or requires zero configuration.

## Security Is a Lifecycle, Too

Security is not finished when login succeeds. UltimateAuth's architecture accounts for what happens *after* access is established:

| Situation | Security question | UltimateAuth concept |
| --- | --- | --- |
| A user signs in | Should this attempt establish authentication? | Login flow and security decisions |
| A session needs renewal | Can continuity be preserved safely? | Refresh flow and session lifecycle |
| A user signs out | Which authentication scope should end? | Logout |
| A device is lost | How can that device's access be terminated? | Chain-level management and revocation |
| A credential is compromised | How should existing authentication be affected? | Credential security state and revocation policies |
| An administrator manages access | Is the operation permitted for this actor and target? | Authorization and access context |

A key distinction is **logout versus revoke**. Ordinary logout ends authentication according to its scope; revocation is an irreversible invalidation of the targeted security state. Signing in again may establish *new* authentication, but it does not restore what was revoked.

Explore the details in [Login Flow](/docs/auth-flows/login), [Refresh Flow](/docs/auth-flows/refresh), and [Logout Flow](/docs/auth-flows/logout).

## Modular and Extensible

UltimateAuth is intended to serve both straightforward applications and systems with more demanding security requirements. Its architecture separates responsibilities such as core contracts, server execution, client integration, user management, credentials, and authorization.

This separation makes it possible to understand and evolve individual concerns without reducing authentication to a collection of disconnected custom handlers.

**Extensible does not mean every possible flow is already implemented.** Available modes, integrations, and capabilities should be evaluated against the version you are using; the documentation for each feature describes its current behavior and limitations.

## When Should You Choose UltimateAuth?

UltimateAuth is especially relevant when:

- You need quickly-installed, consistent, secure and easy-to-use auth in your .NET application.
- Your .NET application has—or will have—**more than one client type**.
- You need **device-aware session management**, not only token generation.
- You are designing a **multi-tenant** system with clear authentication boundaries.
- You want **authentication and authorization** to follow a consistent architectural model.
- You need control over **refresh, logout, revocation, and credential lifecycle**.
- You value a **modular, self-hosted, open-source** foundation you can understand and extend.

Also for a small application with minimal authentication needs, UltimateAuth brings the simplest possible solution.

## Open Source First

UltimateAuth is built with a simple commitment: **self-hosted authentication should remain accessible**. The framework is open source, and self-hosting is intended to remain free. A hosted offering may provide an operational alternative, not a separate tier of authentication features.

The ambition is to build a framework that developers can adopt, inspect, contribute to, and trust—not merely a service they must depend on.

## Where to Go Next

You do not need to understand every internal component before getting started.

**Continue to Quick Start** to set up your first UltimateAuth application. Then explore [Auth Flows](/docs/auth-flows/index) to understand how the pieces work together.

**The idea to remember:** UltimateAuth unifies authentication concepts without erasing their differences. One framework, a consistent lifecycle, and room for the security model your application actually needs.
