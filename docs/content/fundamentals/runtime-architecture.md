---
title: Runtime Architecture
order: 6
group: fundamentals
---

# Runtime Architecture

UltimateAuth is designed to provide a unified authentication architecture across different .NET application environments.

A Blazor Server application, a standalone Blazor WebAssembly application, a native MAUI application, and a resource API may have different execution models, but they should not require entirely separate authentication systems.

UltimateAuth addresses this through a separation between **authentication infrastructure, client integration, hosting, and protected resources**.

This separation allows authentication behavior to remain consistent while adapting to the capabilities and responsibilities of each runtime.

## Architectural overview

At a high level, UltimateAuth consists of several cooperating layers:

- **Core:** Shared authentication contracts, models, and foundational abstractions.
- **Server:** Authentication operations, security decisions, credential management, and persistence coordination.
- **Client:** Client-side integration, authentication requests, and credential lifecycle handling.
- **UAuthHub:** The application hosting UltimateAuth's authentication endpoints and services.
- **Resource applications:** Applications and APIs that consume authenticated identity and enforce authorization.

These are architectural responsibilities, not necessarily separate processes or deployments.

A single application may host multiple responsibilities. Alternatively, they can be distributed across independently deployed services.

```text
                     Application Clients
                ┌──────────┬──────────┐
                │          │          │
           Blazor WASM     MAUI     Other Clients
                │          │          │
                └──────────┼──────────┘
                           │
                           │ HTTP
                           ▼
                      UAuthHub
                           │
                  UltimateAuth Server
                           │
                 Authentication Services
                           │
                  Stores / Providers
                           │
                           ▼
                   Persistent State


                    Protected Resources
                ┌──────────┬──────────┐
                │          │          │
           Resource API   Web App   Other Services
                │          │          │
                └──────────┼──────────┘
                           │
                  Authentication Validation
                           │
                      Authorization
```

This diagram illustrates the separation of responsibilities.

It does not imply that every authenticated resource request must pass through UAuthHub.

Whether a protected resource needs to consult server-side authentication infrastructure depends on the effective authentication mode and its validation requirements.

## Core: shared authentication foundations

The Core layer defines the common concepts used throughout UltimateAuth.

Its responsibility is to provide stable contracts and abstractions without depending on a particular client runtime or application host.

These foundations include concepts such as:

- Authentication modes.
- Authentication and security contexts.
- Authentication results and contracts.
- Domain models and shared abstractions.
- Security-related options and definitions.

The Core layer provides the common vocabulary used by client integrations, server services, and other framework components.

It should not contain assumptions that authentication always occurs through an HTTP endpoint, a browser cookie, or a particular hosting model.

This separation is important because UltimateAuth supports both stateful and stateless authentication strategies.

A shared authentication contract must not implicitly require session persistence when the selected mode does not use sessions.

## Server: authentication execution and lifecycle

The Server layer implements the server-side authentication capabilities.

It coordinates the operations required to establish, continue, validate, and terminate authentication according to the effective mode and security policies.

Its responsibilities include:

- Authentication flow execution.
- Credential validation and management.
- Session and token lifecycle operations.
- Authentication security decisions.
- Coordination with persistence providers.
- Credential issuance and validation.
- Integration with server-side authorization infrastructure.

The Server layer contains multiple cooperating services rather than one universal authentication engine responsible for every decision.

For example, session lifecycle operations, credential verification, and authorization may follow different execution paths while sharing common security contracts.

The architecture separates orchestration from the underlying services that perform domain-specific work.

For the detailed execution pipeline, see [Auth Flows](/docs/auth-flows/overview).

## Client: runtime integration

The Client layer provides the integration between an application and UltimateAuth's authentication services.

Its purpose is to reduce the amount of authentication infrastructure that application developers need to implement themselves.

Depending on the supported runtime and configuration, client integration may handle:

- Initiating authentication flows.
- Communicating with UAuthHub.
- Processing authentication outcomes.
- Managing supported credential delivery mechanisms.
- Coordinating authentication state with the application.
- Integrating authenticated requests with protected resources.

The client SDK does not replace server-side authentication validation.

It provides an application-facing interface to the authentication infrastructure.

### Client integration is runtime-aware

Different client runtimes have different capabilities.

A browser-based WebAssembly application executes within browser security restrictions.

A native MAUI application has access to platform-specific capabilities.

A Blazor Server application executes application logic on the server while maintaining an interactive connection with the browser.

UltimateAuth accounts for these differences through runtime-specific integrations and Client Profiles.

The authentication model remains shared even when the implementation of credential transport or application state differs.

See [Client Profiles](/docs/fundamentals/client-profiles).

## UAuthHub: the authentication host

UAuthHub is the application hosting UltimateAuth's server-side authentication infrastructure.

It provides the environment in which authentication endpoints and the required server services are available.

Depending on the deployment, UAuthHub can be responsible for:

- Accepting authentication requests.
- Resolving the applicable tenant and authentication context.
- Executing authentication flows.
- Managing credentials and sessions.
- Issuing authentication credentials.
- Applying configured authentication policies.
- Exposing the supported authentication endpoints.

UAuthHub is a hosting role, not an authentication mode.

A single UAuthHub can support multiple client environments and authentication strategies, subject to its configuration.

### Centralized hosting

In a centralized deployment, several applications can use the same UAuthHub.

```text
Blazor WASM Application ──┐
                          │
MAUI Application ─────────┼──► UAuthHub
                          │        │
Web Application ──────────┘        │
                                   ▼
                         Authentication Services
                                   │
                                   ▼
                         Authentication Storage
```

This allows applications to share authentication infrastructure without requiring each application to implement its own credential management and authentication lifecycle.

However, centralized authentication hosting does not mean that all protected resource requests must be routed through UAuthHub.

The resource authentication strategy remains mode-dependent.

## Resource applications and APIs

A resource application is an application or service that exposes functionality requiring authentication and authorization.

Examples include:

- A protected Web API.
- An administrative web application.
- A business application backend.
- An independently deployed service.
- A server-rendered application with protected operations.

A resource application must establish the caller's authenticated identity before allowing access to protected functionality.

It must then enforce the relevant authorization policies.

### Authentication validation depends on the mode

In PureOpaque and Hybrid, authentication requires server-side session authority.

The resource authentication path must therefore have access to the infrastructure necessary to enforce the relevant session validation requirements.

In SemiHybrid and PureJwt, normal access-token authentication uses JWT validation without a mandatory session lookup.

This can allow a resource API to validate access tokens locally when it has the required verification keys and token-validation configuration.

The authentication mode therefore affects the resource application's infrastructure dependencies.

### Authorization remains local to the protected operation

Authentication establishes who the caller is.

Authorization determines whether that caller may perform the requested operation.

A valid authentication credential does not automatically grant permission to every resource.

Resource applications must continue to enforce their authorization requirements through the applicable framework and application policies.

UltimateAuth's authentication integration should preserve these boundaries rather than replacing resource authorization with client-side assumptions.

## Deployment topology

UltimateAuth separates architectural responsibilities from physical deployment boundaries.

This means the same authentication architecture can support different deployment topologies.

### Integrated deployment

An integrated deployment hosts authentication infrastructure and application functionality within the same server application.

```text
ASP.NET Core Application
          │
          ├── UltimateAuth Server
          │
          ├── Authentication Services
          │
          ├── Application Services
          │
          └── Protected Endpoints
```

In this model, authentication services and protected application functionality may share the same dependency injection container and process.

Where supported, internal operations can interact with server-side services directly without requiring an HTTP call to the same application.

This can simplify deployment and avoid unnecessary network communication.

However, direct service access must still respect the required authentication and authorization boundaries.

### Distributed deployment

A distributed deployment separates the authentication host from one or more resource applications.

```text
                    UAuthHub
                       │
                       │ Authentication
                       │
           ┌───────────┴───────────┐
           │                       │
      Web Application         Mobile Client
           │                       │
           └───────────┬───────────┘
                       │
                       ▼
                   Resource API
                       │
                       ▼
               Application Services
```

In this model, clients communicate with UAuthHub for the applicable authentication operations and access resource APIs using the supported credentials.

Resource APIs validate those credentials according to the configured authentication mode.

This architecture supports independently deployed applications while maintaining shared authentication policies and identity infrastructure.

### Choosing a topology

Integrated and distributed deployments are not different authentication modes.

They describe where application components execute and how they communicate.

The appropriate topology depends on:

- Application boundaries.
- Deployment and operational requirements.
- Network and infrastructure constraints.
- Authentication mode dependencies.
- Service ownership and scaling requirements.

A distributed deployment is not inherently more secure or more scalable than an integrated deployment.

Both require correct authentication validation, authorization enforcement, and infrastructure configuration.

## Blazor Server architecture

Blazor Server has a distinct execution model.

The browser maintains an interactive connection to an application whose component logic executes on the server.

This makes it possible to keep many application operations and authentication-related services within the server environment.

```text
Browser
   │
   │ Interactive Connection
   ▼
Blazor Server Application
   │
   ├── UI Components
   │
   ├── Application Services
   │
   ├── UltimateAuth Integration
   │
   └── Protected Operations
```

PureOpaque is a natural default for this environment because it provides server-controlled authentication without requiring a separate access/refresh token lifecycle.

However, the hosting model does not require PureOpaque exclusively.

### Direct service access

When authentication infrastructure and application services are hosted in the same process, server-side code may be able to use registered services directly.

This can avoid unnecessary HTTP requests between components of the same application.

The distinction is between an application service call and a remote API request.

A direct service call must not bypass security checks merely because it avoids HTTP.

Authorization requirements must still be enforced at the appropriate trusted boundary.

### Long-lived interactive connections

Blazor Server introduces an additional consideration: authentication state may change while an interactive circuit remains active.

A session can expire or be revoked after the circuit was established.

Applications must therefore account for authentication-state revalidation and authorization of sensitive operations.

An authenticated circuit must not be treated as permanently authorized solely because its initial connection succeeded.

## Blazor WebAssembly architecture

Blazor WebAssembly executes application code in the browser.

Unlike Blazor Server, it cannot directly access private server-side authentication services.

Authentication and protected application operations therefore cross an HTTP boundary.

```text
Browser
   │
   └── Blazor WebAssembly
             │
             ├── UltimateAuth Client
             │        │
             │        ▼
             │      UAuthHub
             │
             └── Application API Client
                      │
                      ▼
                  Resource API
```

Hybrid is a suitable authentication strategy for applications that need separate access and refresh tokens while retaining server-authoritative session validation.

However, a browser application can also use other modes when the corresponding transport and security architecture supports them.

For example, a backend-for-frontend architecture may use secure session cookies rather than exposing bearer tokens to browser application code.

The runtime determines which integration techniques are available; the authentication mode determines the security and lifecycle model.

## Native application architecture

Native applications, including .NET MAUI applications, execute outside the browser environment.

They generally communicate with authentication hosts and resource APIs through HTTP.

```text
MAUI Application
      │
      ├── UltimateAuth Client
      │        │
      │        ▼
      │      UAuthHub
      │
      ├── Secure Credential Storage
      │
      └── Resource API Client
               │
               ▼
           Resource API
```

Native applications can use platform-specific secure storage for sensitive credentials.

Their credential delivery and persistence requirements differ from browser applications.

Hybrid can provide a suitable model for applications requiring refresh-token continuity and centralized session control.

The choice remains configurable according to the supported authentication modes and application requirements.

## Shared components across runtimes

A unified authentication architecture should minimize unnecessary duplication between client environments.

UltimateAuth separates shared authentication behavior from runtime-specific integration.

For example, Blazor Server and Blazor WebAssembly applications may share:

- Authentication-related UI components.
- Login and logout interaction patterns.
- Request and response contracts.
- Application-facing client abstractions.
- Authentication-state presentation logic.

At the same time, they may require different implementations for:

- Credential storage.
- HTTP communication.
- Browser interaction.
- Server-side service access.
- Authentication-state synchronization.

The goal is not to force every runtime to execute identical code.

The goal is to share behavior and contracts where appropriate while preserving each runtime's security and execution requirements.

## HTTP boundaries and direct execution

An important architectural distinction is whether an operation crosses an HTTP boundary.

### Remote execution

A remote client communicates with an authentication host or resource API through HTTP.

The server must independently validate the request, credentials, and authorization requirements.

Client-side checks are not sufficient.

### In-process execution

An application may host UltimateAuth services and protected application services within the same process.

In that case, some operations may use direct service calls instead of HTTP.

This can reduce serialization and network overhead.

However, HTTP middleware and endpoint authorization attributes do not automatically execute during arbitrary direct service calls.

The application must ensure that equivalent security requirements are enforced through the appropriate service or authorization boundary.

Direct execution is an optimization and integration choice, not permission to bypass authentication or authorization.

## Infrastructure and persistence

UltimateAuth uses abstractions to separate authentication behavior from infrastructure implementations.

Depending on the configured mode and services, the system may require storage for:

- User and credential information.
- Authentication security state.
- Session lineage.
- Refresh-token lifecycle.
- Device-associated authentication state.

These dependencies vary by authentication mode.

For example, PureOpaque and Hybrid require session infrastructure for authentication.

SemiHybrid requires session infrastructure for lifecycle management but not for normal JWT validation.

PureJwt does not require server-side session infrastructure. Optional refresh-token functionality may introduce independent renewal-state requirements.

The architecture must preserve these distinctions.

Infrastructure abstractions allow the authentication services to remain independent of a particular persistence implementation, while the selected mode determines which capabilities are required.

## Architectural boundaries

UltimateAuth's runtime architecture depends on several boundaries remaining explicit.

### Client and server

Clients initiate operations and present credentials.

Servers establish trusted authentication state and enforce security requirements.

### Authentication and authorization

Authentication determines identity.

Authorization determines whether an operation is permitted.

### Hosting and authentication mode

UAuthHub and resource applications describe hosting responsibilities.

PureOpaque, Hybrid, SemiHybrid, and PureJwt describe authentication strategies.

### Shared contracts and runtime integration

Shared contracts define consistent behavior.

Runtime integrations adapt that behavior to browser, native, and server-side environments.

### Orchestration and infrastructure

Orchestration coordinates authentication operations.

Infrastructure services provide persistence, token operations, and other required capabilities.

These boundaries allow UltimateAuth to evolve without requiring every client integration or authentication mode to duplicate the complete authentication implementation.

## Summary

UltimateAuth's runtime architecture separates authentication responsibilities from application hosting and client execution environments.

Its main architectural components are:

- **Core**, which defines shared contracts and authentication foundations.
- **Server**, which implements authentication operations and lifecycle management.
- **Client**, which provides application-facing authentication integration.
- **UAuthHub**, which hosts the authentication infrastructure.
- **Resource applications**, which authenticate callers and enforce authorization.

These components can operate within an integrated application or across distributed services.

Different client runtimes can share authentication contracts and behavior while using integration mechanisms appropriate to their environments.

Authentication modes determine the required validation and state dependencies.

**The architecture is unified because its security contracts and responsibilities remain consistent—not because every runtime, mode, or deployment follows an identical execution path.**

## Next step

Continue to [Request Lifecycle](/docs/fundamentals/request-lifecycle) to understand how an incoming request moves through authentication validation, identity establishment, and authorization before reaching a protected operation.