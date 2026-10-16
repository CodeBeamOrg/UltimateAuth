---
title: Plugin Domains
order: 1
group: plugin-domains
---

# Plugin Domains

UltimateAuth separates **authentication infrastructure** from the domain capabilities commonly required by identity and access management systems.

The authentication engine is responsible for establishing and validating authentication state. Managing users, credentials, and authorization policies introduces additional responsibilities that do not inherently belong to that engine.

UltimateAuth organizes these capabilities into **Plugin Domains**: modular, independently structured domains that extend the framework beyond authentication without making their business logic part of the authentication core.

Plugin Domains are an architectural concept specific to UltimateAuth. They are not simply runtime plugins or optional features within a monolithic identity system.

## Authentication Core and Plugin Domains

The distinction is based on responsibility rather than importance.

**Authentication Core** provides the infrastructure for authentication flows, authentication modes, sessions, tokens, and runtime security boundaries.

**Plugin Domains** provide the identity and access management capabilities built around that infrastructure.

The framework currently organizes these responsibilities into three primary domains:

| Domain | Responsibility |
|---|---|
| **Users** | User lifecycle, identifiers, profiles, and account management |
| **Credentials** | Credential registration, management, verification, and recovery |
| **Authorization** | Permissions, roles, policies, and access management |

These domains are not universally required for every authentication scenario. An application may already have its own identity infrastructure or may need only a subset of UltimateAuth's domain capabilities.

However, together they provide the foundation for using UltimateAuth as a broader identity and access management platform rather than only an authentication engine.

## Architectural Boundaries

Each Plugin Domain owns a distinct area of business logic.

A domain should not need to understand the internal implementation of another domain to perform its responsibilities.

This separation establishes several architectural properties:

- **Independent domain boundaries:** Each domain defines and maintains its own contracts and behavior.
- **Explicit integration:** Cross-domain interactions occur through defined abstractions and integration points.
- **Replaceable infrastructure:** Storage implementations and supporting infrastructure can be substituted without changing the authentication engine.
- **Controlled extensibility:** Applications can customize domain behavior while preserving established framework contracts.

Isolation does not mean that domains never interact. Identity management operations frequently involve multiple domains, and those interactions must remain coordinated.

UltimateAuth addresses this through explicit integration contracts rather than direct dependencies between domain implementations.

## Domain Package Structure

Plugin Domains follow a layered package structure that separates integration contracts, public models, domain behavior, and persistence.

| Layer | Purpose |
|---|---|
| **Bridge** | Minimal abstractions required for integration with UltimateAuth Server |
| **Contracts** | Shared requests, responses, and data transfer models |
| **Reference Implementation** | Default application services, domain behavior, and storage abstractions |
| **Persistence** | Concrete storage implementations, including InMemory and Entity Framework Core |

This structure keeps the authentication server independent of the full internal implementation of each domain.

It also allows applications to use the provided implementations while retaining control over infrastructure and customization.

The reference implementations are intended to be practical foundations for real applications, not merely illustrative examples.

## Cross-Domain Integration

Some identity operations naturally cross domain boundaries.

For example, creating a user may need to initiate credential-related behavior. That coordination should not require the Users domain to reference the Credentials domain directly.

Instead, UltimateAuth uses integration abstractions and lifecycle hooks to connect independently implemented responsibilities.

`IUserLifecycleIntegration` is an example of such an integration point.

This approach allows domain-specific behavior to remain within its owning domain while supporting coordinated application workflows.

Integration hooks are explicit extension boundaries, not a mechanism for bypassing domain ownership or security rules.

## Extending Plugin Domains

UltimateAuth provides reference implementations so applications do not need to rebuild common identity management capabilities from scratch.

For most applications, the recommended approach is to **start with the provided domain implementations and customize only what is necessary**.

Depending on the domain and its supported extension contracts, customization may include:

- Replacing persistence implementations.
- Providing application-specific services.
- Extending supported lifecycle integrations.
- Integrating existing identity or access management infrastructure.

Replacing an entire domain is possible as an architectural objective, but doing so requires preserving the contracts expected by the rest of the system.

Modularity provides flexibility; it does not eliminate the need for compatible behavior.

## Choosing the Right Capabilities

Plugin Domains are designed to be composed according to application requirements.

An application focused on authentication infrastructure may rely primarily on the Core and Server capabilities.

An application requiring complete identity management can incorporate Users, Credentials, and Authorization together.

The important distinction is that **the authentication engine does not need to become a monolithic identity management system to support either scenario**.

This separation allows UltimateAuth to serve both focused authentication use cases and more comprehensive identity platforms while maintaining clear architectural boundaries.

## Explore the Domains

Continue with the individual domains to understand their responsibilities, public contracts, and extension points.

- [Users](/docs/plugin-domains/users) — User identities, identifiers, and lifecycle management.
- [Credentials](/docs/plugin-domains/credentials) — Credential management, verification, and recovery.
- [Authorization](/docs/plugin-domains/authorization) — Roles, permissions, policies, and access control.

For the underlying authentication architecture, see [Runtime Architecture](/docs/fundamentals/runtime-architecture) and [Authentication Model](/docs/fundamentals/authentication-model).