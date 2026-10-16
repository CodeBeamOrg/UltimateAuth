---
title: Getting Started
order: 1
group: getting-started
---

# Welcome to UltimateAuth

**Modern authentication and authorization for .NET — built around a unified security lifecycle.**

Authentication starts with a login. But it doesn't end there.

As applications grow, they introduce new challenges: multiple clients, refresh tokens, device sessions, credential recovery, tenant isolation, permissions, and revocation. What begins as a simple login system can gradually become a collection of disconnected security mechanisms.

UltimateAuth takes a different approach.

It brings authentication, authorization, users, credentials, sessions, and client integration into **one coherent framework**, with a shared model for how security decisions are made and how authentication state changes over time.

**One framework. Multiple client environments. A consistent security lifecycle.**

[Explore the Source](https://github.com/CodeBeamOrg/UltimateAuth)

## Why UltimateAuth?

Most applications don't struggle with generating an access token.

They struggle with everything that comes afterward.

Consider an application that starts with a single web frontend. Eventually, it adds a mobile application, an administration panel, multiple organizations, and external APIs.

Suddenly, authentication raises questions that a basic login implementation doesn't answer:

- How should browser sessions and API tokens coexist?
- Can users see and revoke sessions associated with their devices?
- What happens to existing sessions after a password reset?
- How do we prevent refresh token replay?
- How do we enforce tenant boundaries consistently?
- How do we keep authorization decisions separate from authentication?
- Can multiple application types use the same backend security model?

These concerns are related. Implementing each independently makes the system harder to maintain, audit, and extend.

**UltimateAuth is designed to manage these concerns as parts of the same security lifecycle.**

## A Different Way to Think About Authentication

Traditional authentication integration is often organized around credentials and their immediate results:

```text
Credentials
    ↓
Validate
    ↓
Issue Token
    ↓
Authenticated
```

That sequence is necessary, but incomplete.

UltimateAuth approaches authentication as a longer-running lifecycle:

```text
                    UltimateAuth
                         │
                         ▼
                Request Context
            Client · Tenant · Device
                         │
                         ▼
                 Authentication
                Validation · Decisions
                         │
                         ▼
                 Security State
              User · Root · Chain · Session
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
           Refresh     Logout     Revoke
              │          │          │
              └──────────┼──────────┘
                         ▼
                 Controlled State
                    Transitions
```

This model makes it possible to reason about authentication beyond a single request.

Login establishes authentication. Refresh evaluates whether it may continue. Logout and revocation terminate authentication at the appropriate scope. Credential changes and security policies can affect existing sessions.

The result is a framework where these operations share a consistent security vocabulary.

## What Makes UltimateAuth Different?

<escape>| Capability | UltimateAuth's approach |
|---|---|
| **Unified architecture** | Authentication, sessions, users, credentials, and authorization share a common foundation |
| **Client-aware behavior** | Authentication can adapt to the client profile and effective server configuration |
| **Session lifecycle** | Root, chain, and session concepts support different scopes of session management |
| **Flow-driven execution** | Authentication operations use explicit context, validation, decisions, and state transitions |
| **Credential lifecycle** | Credential changes, recovery, and session invalidation are related security operations |
| **Tenant awareness** | Tenant context participates in authentication and access decisions |
| **Modularity** | Core, Server, Client, and domain packages separate responsibilities |
| **Self-hosting** | Applications can operate their own authentication infrastructure |

</escape>

UltimateAuth does not claim that tokens, sessions, PKCE, or policy-based authorization are new concepts.

Its value is in **bringing established security concepts together under a consistent architecture**, rather than requiring every application to connect them independently.

## One Security Model Across Different Clients

A browser application, a server-rendered application, and a native client have different security constraints.

They should not necessarily use identical credential storage, transport, or authentication flows.

UltimateAuth separates the common authentication model from client-specific integration.

<escape>```text
            Your .NET Applications

    Blazor Server      Blazor WebAssembly
          │                    │
          └─────────┬──────────┘
                    │
             UltimateAuth Client
                    │
                    ▼
             UltimateAuth Server
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
      Users      Sessions   Authorization
        │           │           │
        └───────────┼───────────┘
                    ▼
             Shared Security
                 Lifecycle
```</escape>

The architecture also provides foundations for .NET MAUI, traditional ASP.NET Core applications, and API-oriented integrations.

**Support depends on the client adapter and authentication modes available in the installed version.** A supported architectural profile should not be confused with a fully released platform adapter.

The goal is to share authentication concepts without pretending that all clients have the same trust boundaries.

## Sessions Are More Than Tokens

One of UltimateAuth's central concepts is its stateful session hierarchy.

```text
User
 └── Root
      ├── Chain — Browser A
      │    ├── Session
      │    └── Session
      │
      └── Chain — Device B
           ├── Session
           └── Session
```

Each level represents a different security scope.

**Root** represents a broader authentication security boundary.

**Chain** represents the continuity of authentication associated with a device or client context.

**Session** represents an individual authentication instance within that chain.

This distinction makes several operations easier to express:

- End the current session.
- Revoke a particular device-associated chain.
- Revoke other active chains while preserving the current one.
- Revoke a broader authentication scope after a security-sensitive event.

A token is a credential used to access the system. It is not necessarily the complete record of the user's authentication lifecycle.

The hierarchy applies to session-backed authentication modes; different modes can have different persistence and validation characteristics.

[Explore Session Management](https://github.com/CodeBeamOrg/UltimateAuth)

## Authentication Flows, Not Disconnected Endpoints

UltimateAuth organizes security-sensitive operations around explicit flows.

A flow can combine:

1. Request and tenant context.
2. User, credential, and session resolution.
3. Input and security validation.
4. Authorization and authority decisions.
5. Controlled state transitions.
6. Client-appropriate results and responses.

```text
Request
   ↓
AuthFlowContext
   ↓
Validation
   ↓
Security Decision
   ↓
Execution
   ↓
State Transition
   ↓
Response
```

This approach provides a consistent place for security rules to be evaluated.

It also makes individual operations easier to test and inspect because the important decisions are part of an explicit execution model.

**The purpose is not abstraction for its own sake. It is to make authentication behavior understandable and predictable.**

## Security Continues After Login

A secure authentication system must account for events that happen after a user signs in.

| Event | UltimateAuth concept |
|---|---|
| A user signs in | Login flow |
| A session needs renewal | Refresh flow |
| A user signs out | Logout flow |
| A device should lose access | Chain revocation |
| A credential is changed | Credential lifecycle and session policy |
| An account is recovered | Recovery proof and session invalidation |
| A user requests a protected resource | Authorization decision |
| A request targets a tenant | Tenant-aware security context |

This is why UltimateAuth treats session management, credential management, and authorization as connected parts of the system.

For example, a password reset is not simply a database update. It can also affect the validity of existing authentication sessions according to the configured credential session policy.

Similarly, logout and revocation are not interchangeable operations. They have different scopes and consequences.

## Authentication and Authorization Together

Knowing who a user is does not determine what that user is allowed to do.

UltimateAuth separates authentication from authorization while keeping them within the same overall security architecture.

Authentication establishes identity and applicable security context.

Authorization evaluates whether an actor may perform an action on a resource.

For example:

```text
Actor:    Authenticated user
Action:   update
Resource: products
Result:   Allowed or Denied
```

Roles and permissions can be managed through the authorization domain, while the server remains responsible for enforcing access decisions.

This separation helps applications avoid spreading authorization logic across UI components, controllers, and unrelated service methods.

Client-side authorization checks can improve the interface, but **server-side enforcement remains the security boundary**.

## Tenant-Aware by Design

Multi-tenant authentication involves more than adding a tenant identifier to a user record.

A request must be interpreted within the correct tenant context, and security operations must respect that boundary.

UltimateAuth provides tenant resolution and propagation mechanisms that participate in its request and flow infrastructure.

This allows applications to build tenant-aware authentication and authorization behavior without treating tenancy as an unrelated afterthought.

Tenant context must still be configured correctly, and a client-supplied tenant identifier is never sufficient proof of authorization.

## Modular Without Becoming Fragmented

UltimateAuth is divided into packages with distinct responsibilities.

| Package or domain | Responsibility |
|---|---|
| **Core** | Shared contracts, domain concepts, and authentication foundations |
| **Server** | Authentication execution, runtime configuration, and server integration |
| **Client** | Platform-independent authentication and domain APIs |
| **Client.Blazor** | Blazor-specific transport, storage, and authentication state integration |
| **Users** | User lifecycle, profiles, and identifiers |
| **Credentials** | Credential operations, changes, and recovery |
| **Authorization** | Roles, permissions, and access checks |
| **Policies** | Application access policy integration |

This structure supports both a straightforward integration experience and deeper customization where the application requires it.

Developers can begin with the public registration and client APIs without first learning every internal service.

When necessary, they can explore the underlying flows, policies, stores, and extension points.

## Designed for Practical .NET Development

UltimateAuth is intended to fit naturally into .NET applications.

A server application begins by registering UltimateAuth and configuring the security behavior it needs:

```csharp
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Session.Lifetime = TimeSpan.FromDays(7);
    options.CredentialSessionPolicy.OnRecovery = CredentialSessionBehavior.RevokeAll;
});
```

A Blazor application registers the client adapter:

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.AutoDetectClientProfile = true;
    options.Endpoints.BasePath = "/auth";
});
```

These examples illustrate the configuration entry points rather than a complete application setup.

The server and client have different responsibilities, and a complete deployment also needs the appropriate hosting, persistence, transport, and authentication integration.

The important idea is that common behavior is configured through structured options rather than scattered application-specific code.

[Start with QuickStart](/docs/getting-started/quickstart)

## Who Is UltimateAuth For?

UltimateAuth is particularly relevant if you are building:

- **A Blazor application** that needs authentication and authorization integrated with its hosting model.
- **A multi-client .NET system** where different frontends should share the same authentication backend.
- **An application with device-aware sessions** where users or administrators need control over active authentication.
- **A multi-tenant platform** with explicit tenant resolution and security boundaries.
- **A security-sensitive application** that requires controlled refresh, credential recovery, and revocation behavior.
- **A self-hosted system** where the team wants visibility into and control over its authentication infrastructure.

**From a simple login page to a complex authentication platform, UltimateAuth is designed to make getting started easy without limiting what comes next.**

You shouldn't need to understand an entire identity framework just to add authentication to a small .NET application. And you shouldn't need to replace your authentication system when that application grows.

UltimateAuth is built for both.

### Simple Applications. Simple Beginnings.

For a small application, authentication should be straightforward.

UltimateAuth aims to provide a lightweight integration experience with clear configuration, sensible defaults, and ready-to-use authentication flows.

Instead of requiring developers to assemble numerous identity components before they can begin, UltimateAuth provides a cohesive foundation that can be configured through a small number of well-defined entry points.

**Start with what you need. You don't have to configure everything the framework supports.**

### Complex Applications. Room to Grow.

When requirements become more demanding, the same framework provides a deeper security architecture.

UltimateAuth is particularly useful for applications requiring multiple client types, device-aware session management, tenant isolation, advanced authorization, credential recovery, controlled refresh, and targeted revocation.

Its modular architecture, explicit authentication flows, configurable policies, and extension points allow developers to introduce additional capabilities without abandoning the original authentication foundation.

**The complexity of your application should not determine how difficult authentication is to begin with.**

### One Framework for Both

Whether you're building a small personal project, a business application, a SaaS platform, or a larger distributed system, UltimateAuth follows the same principle:

**Easy to start. Powerful when needed. Consistent as you grow.**

You can begin with a straightforward authentication setup and progressively adopt the capabilities your application actually requires.

UltimateAuth can also serve smaller applications, but its architecture becomes especially valuable when authentication requirements extend beyond a basic login form.

If your application only needs a minimal authentication mechanism, evaluate whether UltimateAuth's broader lifecycle model provides enough value for your use case.

## Current Scope and Maturity

UltimateAuth is an evolving open-source framework.

Its architecture covers several authentication models and client environments, but not every designed capability is necessarily available in the current release.

In particular:

- Authentication mode availability must be checked against the current server implementation.
- Client adapter availability varies by platform.
- Some advanced session and device policy options may be reserved for future enforcement.
- Production adoption requires appropriate configuration, persistence, deployment security, and testing.

The documentation distinguishes supported behavior from planned or partially implemented capabilities.

This matters because a trustworthy authentication framework should be clear not only about what it can do, but also about its current limits.

## Open Source First. Self-Hosted by Design.

UltimateAuth is developed as an open-source project with self-hosting at its foundation.

The project's direction is to keep self-hosted authentication free, while a potential managed hosting service could provide an alternative for teams that prefer not to operate the infrastructure themselves.

The intention is not to divide authentication capabilities into free and paid feature tiers.

It is to make the framework accessible, inspectable, extensible, and useful to the .NET community.

[View UltimateAuth on GitHub](https://github.com/CodeBeamOrg/UltimateAuth)

## Choose Your Starting Point

You do not need to understand the entire architecture before building your first application.

| I want to… | Start here |
|---|---|
| Set up UltimateAuth for the first time | [QuickStart](/docs/getting-started/quickstart) |
| Understand how authentication works internally | [Auth Flows](/docs/auth-flows/index) |
| Configure session, token, and security behavior | [Server Options](/docs/configuration/server-options) |
| Integrate authentication into a Blazor application | [Client Options](/docs/configuration/client-options) |
| Understand the source code and contribute | [GitHub Repository](https://github.com/CodeBeamOrg/UltimateAuth) |

---

### Authentication is a lifecycle, not a login endpoint.

UltimateAuth brings together the concepts needed to establish, maintain, evaluate, and terminate authentication within a consistent .NET architecture.

**Start simple. Understand the security model. Extend when you need to.**

[Continue to QuickStart](/docs/getting-started/quickstart)