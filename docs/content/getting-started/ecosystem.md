---
title: UltimateAuth Ecosystem
order: 2
group: getting-started
---

# UltimateAuth Ecosystem

UltimateAuth is designed as a modular authentication and identity platform for .NET.

The platform has three primary foundations:

```text
                         UltimateAuth
                              |
                             Core
                              |
                 +------------+------------+
                 |                         |
               Server                    Client
                 |                         |
        Server composition          Client integration
          through bundles          for the application
```

**Core** defines the common language and foundational abstractions of the platform.

**Server** hosts and executes authentication and identity capabilities.

**Client** provides the application-facing model used by applications to interact with those capabilities.

An application does not necessarily need both Server and Client. Depending on its role, it may use:

- Server only
- Client only
- Server and Client together

This separation is intentional. UltimateAuth is designed so that application topology does not dictate a monolithic package structure.

---

## Platform at a Glance

A useful way to think about UltimateAuth is:

```text
UltimateAuth
|
+-- Core
|
+-- Server
|   |
|   +-- Essential server capabilities
|   |   +-- Authentication
|   |   +-- Sessions
|   |   +-- Tokens
|   |   +-- Policies
|   |
|   +-- Plugin Domains
|   |   +-- Users
|   |   +-- Credentials
|   |   +-- Authorization
|   |
|   +-- Implementations
|       +-- Reference
|       +-- InMemory
|       +-- Entity Framework Core
|
+-- Client
    |
    +-- Client.Blazor
    +-- Client.AspNetCore compatibility
    +-- Future client integrations
```

The important distinction is that these packages are not intended to make installation complicated.

The internal architecture is modular, while the installation experience is deliberately composed around **server bundles** and **client-specific packages**.

---

# Core

## `CodeBeam.UltimateAuth.Core`

Core is the foundation of the entire UltimateAuth platform.

It contains the shared primitives, domain concepts, contracts, abstractions, value objects, configuration foundations, and common infrastructure required by the rest of UltimateAuth.

Concepts such as UltimateAuth's identity and session model originate from this layer.

Core is intentionally independent from a particular application type or persistence technology.

```text
                  Core
                   |
          +--------+--------+
          |                 |
        Server            Client
```

Server and Client share the same platform vocabulary because both are built on Core.

Most application developers do not need to install Core directly. It is normally brought into the application through the appropriate Server bundle or Client integration package.

---

# Server

The Server side is the authority that executes UltimateAuth authentication and identity behavior.

It is responsible for the server-side runtime, including authentication flows, security decisions, orchestration, endpoints, middleware integration, and the coordination of server capabilities.

At source level, `CodeBeam.UltimateAuth.Server` is the central server project and depends on major server domains such as:

```text
CodeBeam.UltimateAuth.Server
|
+-- Core
+-- Authorization
+-- Credentials
+-- Users
+-- Policies
```

Other essential server capabilities, including authentication, sessions, and tokens, are kept in focused projects rather than being merged into one oversized server assembly.

This gives UltimateAuth a modular internal architecture without forcing that complexity onto application developers.

## Do I install `CodeBeam.UltimateAuth.Server` directly?

For normal application setup, **no**.

The recommended server installation unit is a **Bundle**.

A bundle represents a complete server composition for a particular infrastructure strategy.

Instead of manually selecting all server packages and implementations, choose the bundle that matches your application.

```text
                     Your Server
                          |
                       Bundle
                          |
          +---------------+---------------+
          |               |               |
        Server        Domains         Infrastructure
          |               |               |
       Runtime         Reference       Persistence
                       behavior         providers
```

This is an important part of the UltimateAuth design:

> The framework remains modular internally, while common server configurations remain simple to install.

---

# Server Bundles

Bundles are server-side composition packages.

They collect the UltimateAuth server capabilities and the implementations required for a particular configuration into a single installation unit.

You normally choose **one server bundle**.

## Reference Bundle

### `CodeBeam.UltimateAuth.Reference.Bundle`

The Reference Bundle provides the recommended UltimateAuth server composition without selecting a persistence implementation for your application.

Use it when you want UltimateAuth's reference behavior but provide your own persistence infrastructure.

Conceptually:

```text
Reference.Bundle
|
+-- Server
+-- UltimateAuth reference implementations
+-- Required server capabilities
|
+-- Persistence: supplied by your application
```

This is the lowest-level recommended server composition for applications that want to own their storage implementation.

It is also an important extensibility point: UltimateAuth provides a recommended architecture without requiring applications to use one particular persistence technology.

## InMemory Bundle

### `CodeBeam.UltimateAuth.InMemory.Bundle`

The InMemory Bundle provides a complete server setup backed by UltimateAuth's in-memory implementations.

It is primarily intended for:

- development
- Quick Start
- samples
- automated tests
- evaluation
- prototypes

```text
InMemory.Bundle
|
+-- Server
+-- Reference implementations
+-- InMemory persistence implementations
```

This is the easiest way to start an UltimateAuth server.

## Entity Framework Core Bundle

### `CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle`

The Entity Framework Core Bundle provides the complete server composition using UltimateAuth's EF Core persistence implementations.

```text
EntityFrameworkCore.Bundle
|
+-- Server
+-- Reference implementations
+-- EF Core persistence implementations
```

For applications that use EF Core for persistent UltimateAuth data, this is normally the server package to install.

The developer does not need to manually assemble the individual Users, Credentials, Authorization, Sessions, Tokens, and other persistence packages.

The bundle performs that composition.

---

# Why Bundles Matter

A highly modular framework can easily make the developer experience worse if every application has to understand and install its internal package graph.

UltimateAuth deliberately separates **architecture modularity** from **installation complexity**.

Internally:

```text
Server
 + Users
 + Credentials
 + Authorization
 + Authentication
 + Sessions
 + Tokens
 + Policies
 + persistence implementations
 + reference implementations
```

From the application:

```bash
dotnet add package CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle
```

This gives UltimateAuth two useful properties at the same time:

1. individual capabilities remain independently replaceable and evolvable;
2. the common path remains simple.

You should not need to understand the complete internal dependency graph before building your first application.

---

# Client

The Client side has a different packaging model.

There are **no Client bundles**.

Instead, applications install the client package designed for their application model.

```text
                    Client Foundation
                           |
             +-------------+-------------+
             |             |             |
           Blazor        MAUI          Future
```

For example, a Blazor application installs:

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

A future MAUI integration can follow the same model without changing the underlying UltimateAuth Client architecture.

## `CodeBeam.UltimateAuth.Client`

`CodeBeam.UltimateAuth.Client` is the shared client foundation.

It contains common client contracts, abstractions, flow APIs, state concepts, and runtime-independent client behavior.

It should be thought of as the foundation on which concrete UltimateAuth client integrations are built.

Most applications should therefore install their platform-specific client package rather than installing `CodeBeam.UltimateAuth.Client` directly.

---

## `CodeBeam.UltimateAuth.Client.Blazor`

`CodeBeam.UltimateAuth.Client.Blazor` is the concrete UltimateAuth client integration for Blazor.

It builds on the Client foundation and adds the Blazor-specific runtime and UI integration required by Blazor applications.

This includes capabilities such as:

- `UAuthApp`
- `UAuthLoginForm`
- `UAuthStateView`
- authentication state integration
- Blazor authorization integration
- routing integration
- authentication flow components
- JavaScript transport integration
- client lifecycle coordination

Conceptually:

```text
Core
 |
Client
 |
Client.Blazor
 |
Your Blazor Application
```

This model keeps the shared Client architecture independent from Blazor while still giving Blazor developers one natural package to install.

---

# Client Compatibility Packages

Not every client-side package represents a complete client implementation.

Some packages exist to bridge UltimateAuth with a host framework.

## `CodeBeam.UltimateAuth.Client.AspNetCore`

`CodeBeam.UltimateAuth.Client.AspNetCore` is a lightweight ASP.NET Core compatibility layer for UltimateAuth client applications.

It allows client-oriented applications to integrate with ASP.NET Core authentication and authorization infrastructure without introducing the full UltimateAuth Server runtime.

This is particularly useful for hosting models such as Blazor Web App with Interactive WebAssembly.

It can enable compatibility with ASP.NET Core features such as `[Authorize]`, but it does **not** implement authentication itself and does not turn the application into an UltimateAuth Server.

---

# Application Composition

Because Server and Client are independent platform sides, different applications can compose UltimateAuth differently.

## Server Only

Typical examples include:

- dedicated authentication servers
- backend services hosting UltimateAuth
- server-side identity infrastructure

```text
Application
|
+-- Server Bundle
```

## Client Only

Typical examples include:

- standalone Blazor WebAssembly
- future MAUI applications
- applications using a remote UAuthHub

```text
Application
|
+-- Client.Blazor
        |
        +---- remote UltimateAuth Server / UAuthHub
```

## Server + Client

Some applications host UltimateAuth and consume its client API in the same application.

Blazor Server is a common example.

```text
Blazor Server Application
|
+-- Server Bundle
|
+-- Client.Blazor
```

The two sides remain architecturally distinct even when deployed in the same process.

---

# Plugin Domains

UltimateAuth extends its identity platform through **Plugin Domains**.

The current primary Plugin Domains are:

- Users
- Credentials
- Authorization

A Plugin Domain is more than a single assembly. It is a capability area with contracts, runtime behavior, recommended implementations, and infrastructure implementations.

This structure allows a domain to remain replaceable without requiring applications to redesign the rest of UltimateAuth.

## Anatomy of a Plugin Domain

A typical Plugin Domain follows this model:

```text
Plugin Domain
|
+-- Contracts
|
+-- Domain / Runtime
|
+-- Reference
|
+-- Persistence
    |
    +-- InMemory
    +-- EntityFrameworkCore
```

Each layer has a distinct purpose.

---

## Contracts

Example:

```text
CodeBeam.UltimateAuth.Users.Contracts
```

Contracts define the public boundary of the domain. Server and Client can communicate through same contracts without requiring one to depend on the other.

They are intentionally kept close to Core and represent the smallest dependency surface for applications or extensions that only need to interact with the domain contract.

Depending on the domain, contracts can contain concepts such as:

- requests
- results
- public models
- abstractions
- domain-facing contracts

This allows another implementation to integrate with UltimateAuth without depending on the entire built-in implementation.

---

## Domain / Runtime

Example:

```text
CodeBeam.UltimateAuth.Users
```

The main domain project provides the runtime and application behavior of that Plugin Domain.

It operates against abstractions rather than requiring one persistence implementation.

---

## Reference Implementations

Example:

```text
CodeBeam.UltimateAuth.Users.Reference
```

Reference packages contain the implementation UltimateAuth recommends as the standard baseline for the domain.

The distinction between **contract** and **reference implementation** is intentional.

UltimateAuth does not leave extensibility points empty and require every developer to design critical identity behavior from scratch.

Instead, the platform provides:

```text
Contract
   |
   +---- UltimateAuth Reference Implementation
   |
   +---- Your Custom Implementation
```

For the common path, use the UltimateAuth reference implementation.

For specialized requirements, replace the appropriate implementation while preserving the surrounding platform contract and security boundaries.

This pattern gives advanced applications extensibility without making simple applications incomplete.

---

## Persistence Implementations

Plugin Domains can provide persistence-specific packages independently.

For example:

```text
Users
|
+-- Users.Contracts
+-- Users
+-- Users.Reference
+-- Users.InMemory
+-- Users.EntityFrameworkCore
```

The same pattern applies to other Plugin Domains where appropriate.

A developer using a server bundle normally does not need to install these packages individually. The selected bundle composes the appropriate persistence strategy.

Their separation exists primarily to preserve architectural modularity and replaceability.

---

# Current Plugin Domains

## Users

The Users domain owns user lifecycle and user-management capabilities.

Its package family includes:

```text
CodeBeam.UltimateAuth.Users.Contracts
CodeBeam.UltimateAuth.Users
CodeBeam.UltimateAuth.Users.Reference
CodeBeam.UltimateAuth.Users.InMemory
CodeBeam.UltimateAuth.Users.EntityFrameworkCore
```

## Credentials

The Credentials domain owns credential-related capabilities and credential management.

Its package family includes:

```text
CodeBeam.UltimateAuth.Credentials.Contracts
CodeBeam.UltimateAuth.Credentials
CodeBeam.UltimateAuth.Credentials.Reference
CodeBeam.UltimateAuth.Credentials.InMemory
CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore
```

## Authorization

The Authorization domain provides UltimateAuth authorization capabilities.

Its package family includes:

```text
CodeBeam.UltimateAuth.Authorization.Contracts
CodeBeam.UltimateAuth.Authorization
CodeBeam.UltimateAuth.Authorization.Reference
CodeBeam.UltimateAuth.Authorization.InMemory
CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore
```

The same architectural pattern makes these capabilities independently evolvable while keeping their public boundaries explicit.

---

# Essential Server Capabilities

Authentication, Sessions, and Tokens are slightly different from Plugin Domains.

They are fundamental parts of an UltimateAuth Server and are not treated as optional identity plugins in the same sense as Users, Credentials, or Authorization.

However, they are still separated into focused projects.

Conceptually:

```text
                     UltimateAuth Server
                            |
       +--------------------+--------------------+
       |                    |                    |
 Authentication          Sessions              Tokens
```

Why separate them if the Server needs them?

Because **required does not have to mean monolithic**.

Keeping these capabilities isolated:

- prevents the central Server project from becoming an oversized implementation assembly;
- keeps responsibilities and dependency boundaries explicit;
- allows implementations to evolve independently;
- makes persistence providers independently composable;
- keeps replacement and testing boundaries smaller.

They can be thought of as **modular server subsystems** rather than full Plugin Domains.

For example, persistence implementations can still exist independently:

```text
Authentication
+-- Authentication.InMemory
+-- Authentication.EntityFrameworkCore

Sessions
+-- Sessions.InMemory
+-- Sessions.EntityFrameworkCore

Tokens
+-- Tokens.InMemory
+-- Tokens.EntityFrameworkCore
```

The selected Server bundle composes the required implementations for the application.

---

# Policies and Security

Some capabilities are deliberately isolated even though they support multiple areas of the platform.

## Policies

`CodeBeam.UltimateAuth.Policies` contains policy infrastructure used to keep security and application decisions explicit and extensible.

Policies are part of the server architecture rather than application-specific transport logic.

## Security Implementations

Security algorithms can also live behind focused implementations.

For example:

```text
CodeBeam.UltimateAuth.Security.Argon2
```

Keeping security implementations separate avoids coupling the entire platform to one concrete algorithm or provider.

---

# Modularity Without an Incomplete Framework

UltimateAuth's modularity has an important design goal:

> Extensibility should not require developers to build the missing half of the framework themselves.

Every major extensibility boundary should have a practical UltimateAuth-provided path.

The common model is:

```text
                 UltimateAuth Contract
                         |
             +-----------+-----------+
             |                       |
       Reference Path          Custom Path
             |                       |
     Works out of the box      Replace when needed
```

This means the platform can provide strong defaults and reference implementations while still allowing advanced applications to replace individual capabilities.

A customization should be local to the capability being replaced rather than requiring the authentication architecture to be rewritten.

---

# Replaceability by Design

UltimateAuth is intentionally built from small architectural boundaries.

The goal is not modularity for its own sake.

The goal is to reduce the cost of change.

An application may start with:

```text
EntityFrameworkCore.Bundle
```

while still having clearly separated domains and infrastructure underneath.

If a specialized application later needs a custom implementation for one capability, the architecture already has a boundary for it.

This provides a different model from choosing between two extremes:

```text
Simple but monolithic
        OR
Flexible but difficult to configure
```

UltimateAuth aims for:

```text
              Simple installation
                     +
             Modular internals
                     +
          Reference implementations
                     +
             Explicit contracts
                     =
        Low-cost customization
```

Security-sensitive extension points still remain subject to UltimateAuth's security invariants and authority boundaries. Replaceability is intended to enable integration and specialization, not to bypass required security decisions.

---

# Choosing Packages

For most developers, package selection should be simple.

## Blazor Server with InMemory persistence

```bash
dotnet add package CodeBeam.UltimateAuth.InMemory.Bundle
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

The application both hosts UltimateAuth and consumes the Blazor client integration.

## Blazor Server with Entity Framework Core

```bash
dotnet add package CodeBeam.UltimateAuth.EntityFrameworkCore.Bundle
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

## Custom persistence

Start from:

```bash
dotnet add package CodeBeam.UltimateAuth.Reference.Bundle
```

and provide the required persistence implementations for your architecture.

## Standalone Blazor WebAssembly

Install the client integration:

```bash
dotnet add package CodeBeam.UltimateAuth.Client.Blazor
```

The authentication authority lives in a remote UltimateAuth Server or UAuthHub.

---

# The Rule of Thumb

You generally do not need to assemble UltimateAuth package-by-package.

For the **Server**:

> Choose the Bundle that matches your infrastructure.

For the **Client**:

> Choose the Client package that matches your application platform.

```text
SERVER
"What infrastructure do I use?"
          |
          +-- Reference.Bundle
          +-- InMemory.Bundle
          +-- EntityFrameworkCore.Bundle


CLIENT
"What kind of application am I building?"
          |
          +-- Client.Blazor
          +-- Client.Maui        (future)
          +-- ...
```

The detailed package graph exists so that UltimateAuth remains extensible.

The bundles and client integrations exist so that you do not have to manage that graph for ordinary applications.

---

# Next

You now have the mental model needed to understand the rest of the UltimateAuth documentation:

```text
Core
 |
 +-- Server --> choose a Bundle
 |
 +-- Client --> choose your platform integration

Server
 |
 +-- Essential server subsystems
 |
 +-- Plugin Domains
      |
      +-- Contracts
      +-- Runtime
      +-- Reference implementations
      +-- Persistence implementations
```

Continue with the [Quick Start](./quickstart.md) to build your first UltimateAuth application.

For persistent storage, UAuthHub, standalone WebAssembly, Resource API, and other deployment models, continue with the [Real-World Setup](./real-world-setup.md).
