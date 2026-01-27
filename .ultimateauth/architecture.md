# UltimateAuth Architecture (Canonical)

This document defines the NON-NEGOTIABLE architectural principles of UltimateAuth.

If any code, documentation, or contribution conflicts with this document, the code is considered incorrect.

This document is intentionally concise. Explanations, examples, and extended discussions belong in separate architecture guides.

## 1. Architectural Scope

This document defines the canonical architectural boundaries and non-negotiable rules of the UltimateAuth framework.
The scope of this document is strictly limited to the authentication and authorization architecture of UltimateAuth.

### 1.1 What This Document Defines

This document defines:

- The core authentication model and its invariants.
- The request pipeline and context model used to derive authentication and authorization state.
- The separation of responsibilities between core layers (Store, Service, Authority, Issuer).
- The boundaries and responsibilities of plugin domains (Users, Credentials, Authorization).
- The session architecture and its role as the primary source of authentication truth.
- The client and runtime interaction model as it relates to authentication flows.
- Security invariants that MUST hold across all implementations.

These rules apply to all UltimateAuth components, including Core, Server, Client, plugin domains, and reference implementations.

---

### 1.2 What This Document Does NOT Define

This document explicitly does NOT define:

- User interface or user experience flows.
- Application-specific business rules.
- Hosting, deployment, or infrastructure concerns.
- Persistence technologies or storage engines.
- Network protocols or transport-level optimizations.
- Product roadmap, feature prioritization, or timelines.
- Documentation structure or tutorial content.

Any concern outside authentication and authorization architecture is considered out of scope.

---

### 1.3 Architectural Authority

This document is the single authoritative source for architectural decisions within UltimateAuth.

If any implementation, documentation, contribution, or automated refactoring conflicts with this document, the implementation is considered incorrect.

Convenience, performance optimizations, or stylistic preferences MUST NOT override the rules defined here.

---

### 1.4 Relationship to Other Documents

- The UltimateAuth Manifesto describes the philosophy, intent, and design values of the framework.
- This document translates those values into concrete, enforceable architectural rules.
- Architecture guides and design documents MAY provide explanations or examples, but MUST NOT redefine or contradict this document.

In case of conflict, this document takes precedence.

---

### 1.5 Intended Audience

This document is intended for:

- Core framework maintainers.
- Contributors modifying authentication-related code.
- Reviewers evaluating architectural correctness.
- Automated tools (including AI-assisted refactoring) operating on the UltimateAuth codebase.

This document is NOT intended as an onboarding guide or end-user documentation.

## 2. Core Authentication Model

UltimateAuth defines authentication as a session-centered, server-authoritative process.
Authentication determines *who* the current actor is and establishes a stable identity context for the duration of a request or session. Authorization decisions are explicitly out of scope for this model and are handled separately.

---

### 2.1 Session as the Source of Truth

In UltimateAuth, sessions are the primary and authoritative representation of authentication state.

- A session represents an authenticated identity.
- Tokens, cookies, and other credentials are transport mechanisms, not identity sources.
- Authentication state MUST be verifiable on the server using session-backed data.

Any authentication model that treats tokens as the primary source of identity is explicitly rejected.

---

### 2.2 Authentication Modes

UltimateAuth supports multiple authentication modes to address different deployment and runtime requirements.

The following authentication modes are defined:

- **PureOpaque**  
  Authentication is fully session-based. No client-readable identity tokens (except session id) are exposed.

- **Hybrid**  
  Session-backed authentication with client-readable tokens used as a performance optimization.

- **SemiHybrid**  
  Session-backed authentication with limited client-side identity representation.

- **PureJwt**  
  Token-centric authentication with server-side validation, used only when session-backed models are not feasible.

Authentication modes define *how authentication state is represented and transported*, not *how identity is verified*.

The selected authentication mode MUST NOT alter core authentication semantics.

---

### 2.3 Client Profiles and Runtime Awareness

UltimateAuth operates across multiple client runtimes (Blazor Server, Blazor WebAssembly, MAUI, MVC, APIs).

Authentication behavior is adapted at runtime using Client Profiles.

- Client Profiles define runtime-specific defaults.
- Client Profiles are automatically detected when possible.
- Runtime-specific behavior MUST NOT require separate authentication models.

All clients participate in the same core authentication model regardless of runtime or platform.

---

### 2.4 Request-Scoped Authentication Evaluation

Authentication state is evaluated per request.

- Each request derives its authentication context independently.
- Authentication evaluation is deterministic and repeatable.
- No implicit or hidden authentication state is allowed.

Request-based evaluation ensures consistent behavior across distributed systems, retries, and concurrent requests.

---

### 2.5 Separation of Authentication and Authorization

Authentication and authorization are strictly separated.

- Authentication establishes identity.
- Authorization evaluates permissions and access decisions.

Authentication MUST NOT embed authorization logic, permission checks, or policy decisions.

Authorization systems MUST rely on authenticated identity provided by the authentication model.

---

### 2.6 Extensibility Without Semantic Drift

The core authentication model is extensible through plugin domains and override points.

Extensibility MUST preserve the semantic guarantees defined in this section.

Custom implementations, alternative storage mechanisms, or runtime-specific optimizations MUST NOT change:

- The session-first nature of authentication.
- The server-authoritative identity model.
- The separation between authentication and authorization.

## 3. Request Pipeline & Context Model

UltimateAuth evaluates authentication and authorization state within a well-defined, deterministic request pipeline.
This pipeline is responsible for producing immutable context objects that represent the authentication and authorization boundaries of a request.

---

### 3.1 AuthFlowContext

AuthFlowContext represents the complete authentication flow state of a request.

- AuthFlowContext is created exactly once per request.
- AuthFlowContext is request-scoped.
- AuthFlowContext is immutable after creation.
- AuthFlowContext is the single source of truth for authentication-related data during request processing.

AuthFlowContext encapsulates all information required to evaluate authentication state, including session data, client characteristics, and runtime-specific signals.

AuthFlowContext MUST NOT be modified, replaced, or reconstructed after initial creation.

---

### 3.2 AccessContext

AccessContext represents the authorization boundary derived from authentication state.

- AccessContext is derived from AuthFlowContext.
- AccessContext defines *who* the current actor is and *what context* the request is operating under.
- AccessContext is used exclusively for authorization, policy evaluation, and permission checks.

AccessContext MUST NOT contain authentication logic.
Authentication decisions MUST be resolved before AccessContext is created.

---

### 3.3 Context Creation Boundaries

Context creation is a controlled operation within the request pipeline.

- AuthFlowContext creation is part of the authentication pipeline and occurs before application logic executes.
- AccessContext creation is a deterministic transformation of AuthFlowContext.
- Context objects MUST NOT be created lazily or on demand within application or domain code.

Application code, services, and stores MUST treat AuthFlowContext and AccessContext as read-only inputs.

---

### 3.4 Request Determinism

Authentication evaluation in UltimateAuth is deterministic and request-based.

- Each request independently derives its authentication and authorization context.
- No implicit cross-request authentication state is allowed.
- Retried, concurrent, or replayed requests MUST produce equivalent authentication results given the same inputs.

Deterministic evaluation ensures predictable behavior across distributed systems and asynchronous execution.

---

### 3.5 Pipeline Integration Model

UltimateAuth integrates with host frameworks through explicit pipeline extension points.

- Authentication state is established before endpoint execution.
- Context creation occurs as part of the request pipeline, not within application logic.
- Application endpoints MUST NOT be responsible for constructing or mutating authentication context.

The exact integration mechanism (e.g. middleware, endpoint filters, or framework-specific hooks) is an implementation detail and MUST preserve the semantic guarantees defined in this section.

---

### 3.6 Architectural Invariants

The following invariants MUST hold for all implementations:

- Exactly one AuthFlowContext exists per request.
- AuthFlowContext is immutable after creation.
- AccessContext is derived from AuthFlowContext and never the inverse.
- Application and domain code MUST NOT influence authentication context creation.
- Context objects define security boundaries and MUST NOT be bypassed.

Any implementation that violates these invariants is considered architecturally incorrect.

## 4. Domain Boundaries

UltimateAuth is composed of clearly separated domains with explicit responsibilities and non-overlapping concerns.
Domains define behavioral and security boundaries. They MUST NOT be merged, partially implemented, or implicitly coupled.

---

### 4.1 User-Centric Domains

User-related concerns are intentionally split into multiple independent domains.

The following domains exist:

- **UserLifecycle**  
  Represents user existence and security-relevant state (e.g. active, disabled, deleted).

- **UserProfile**  
  Represents user-facing profile and presentation data. This domain MUST NOT affect authentication decisions.

- **UserIdentifier**  
  Represents login identifiers (e.g. email, phone, username) and their verification lifecycle.
  This domain does NOT contain secrets or credentials.

Each domain has its own lifecycle, persistence model and invariants.

No domain may directly modify the state of another domain.

---

### 4.2 Credentials Domain

The Credentials domain is responsible for secret material used to prove identity.

- Credentials are not user profiles.
- Credentials are not identifiers.
- Credentials are security-critical and isolated by design.

Credential types (e.g. password, passkey, OTP) are modeled as distinct domain concepts, independent of storage layout.

---

### 4.3 Authorization Domain

The Authorization domain evaluates permissions and access decisions based on authenticated identity.

- Authorization depends on authentication state.
- Authentication MUST NOT depend on authorization.
- Authorization logic MUST NOT leak into other domains.

---

### 4.4 UserKey as a Cross-Domain Identity Anchor

All user-related domains are linked through a shared UserKey.

UserKey is a value object that represents a stable, opaque identity anchor across domains.

- UserKey is NOT a domain itself.
- UserKey does NOT represent persistence identity.
- UserKey does NOT expose internal structure to domains.
- Domains MUST treat UserKey as an opaque identifier.

Mapping between UserKey and application-specific user identifiers occurs exclusively at system boundaries.

The internal representation of UserKey MUST remain flexible and replaceable without affecting domain logic.

---

### 4.5 Domain Independence Guarantees

The following guarantees MUST hold:

- Domains share UserKey but do NOT share state.
- Domain lifecycles are independent.
- Persistence concerns MUST NOT redefine domain boundaries.
- Cross-domain operations MUST be coordinated at the service or orchestration layer.

Violating domain boundaries is considered an architectural error.

## 5. Store, Service, Orchestrator, and Authority Separation

UltimateAuth enforces a strict separation of responsibilities between persistence, application coordination, orchestration and security decision-making.
Each layer has explicit constraints and MUST NOT assume responsibilities belonging to another layer.

---

### 5.1 Stores

Stores are persistence-only components.

- Stores handle data access and persistence.
- Stores MUST NOT contain authorization logic.
- Stores MUST NOT evaluate policies or permissions.
- Stores MUST NOT create or modify authentication or authorization context.
- Stores MUST NOT depend on AccessContext or AuthFlowContext.

Stores are deterministic and side-effect free beyond their persistence responsibility.

---

### 5.2 Services

Services represent application-level use cases.

- Services define *what* operation is being performed.
- Services coordinate high-level workflows.
- Services invoke orchestrators to execute security-sensitive operations.

Services MUST NOT bypass orchestrators or authorities.

Services are not security boundaries.

---

### 5.3 Orchestrators

Orchestrators coordinate complex, security-critical flows
across multiple domains and subsystems.

- Orchestrators are policy-aware.
- Orchestrators enforce sequencing, invariants and cross-domain consistency.
- Orchestrators interact with Authority components to evaluate security decisions.

UltimateAuth defines multiple orchestrators, including but not limited to:

- Session Orchestrator
- Access Orchestrator
- Login Orchestrator

The existence of multiple orchestrators is intentional.
New orchestrators MAY be introduced as the system evolves.

---

### 5.4 Authority Components

Authority components are responsible for making security and authorization decisions.

- Authorities evaluate policies and permissions.
- Authorities validate whether an operation is allowed.
- Authorities are the final decision point for security-sensitive actions.

Authority logic MUST NOT be embedded in services, stores or domain models.

---

### 5.5 Mandatory Orchestration Rule

All security-relevant operations MUST pass through an orchestrator and an authority.

No operation that affects authentication state, authorization decisions, session validity, credentials, or user security state may be executed without explicit orchestration and authority evaluation.

Bypassing orchestrators or authorities is considered a critical architectural violation.

---

### 5.6 Architectural Guarantees

The following guarantees MUST hold:

- Stores are never policy-aware.
- Services never perform security decisions directly.
- Orchestrators always coordinate security-sensitive flows.
- Authorities are the single source of truth for authorization decisions.
- No execution path may bypass orchestrators and authorities.

Violations of these guarantees compromise system security and are not permitted.

## 6. Session Architecture

UltimateAuth defines sessions as the primary and authoritative representation of authenticated identity.

Sessions establish continuity, revocation guarantees, and server-side control over authentication state.

---

### 6.1 Session as an Authentication Primitive

A session represents an authenticated identity and its associated security state.

- Sessions are server-owned and server-validated.
- Sessions define authentication continuity across requests.
- Sessions are the authoritative source of authentication truth.

Tokens, cookies, or other client-held artifacts are transport mechanisms and MUST NOT be treated as identity sources.

---

### 6.2 Session Types and Composition

UltimateAuth supports structured session composition.

- **Root Sessions** represent the primary authenticated identity.
- **Chained Sessions** represent derived or delegated authentication contexts.

Chained sessions MUST be traceable to a root session and MUST NOT exist independently.

Session composition enables controlled delegation, refresh flows, and security isolation without duplicating identity state.

---

### 6.3 Session Validation and Resolution

Session validation is performed on every request that requires authentication.

- Session validity MUST be verified server-side.
- Session resolution MUST be deterministic.
- Cached or inferred session state is not permitted.

Session resolution MUST NOT depend solely on client-held data.

---

### 6.4 Revocation and Invalidation Semantics

Session revocation is a first-class security operation.

- Revoked sessions MUST be rejected immediately.
- Revocation MUST be enforceable across all authentication modes.
- Session invalidation MUST propagate deterministically.

Eventual or best-effort revocation semantics are explicitly rejected.

---

### 6.5 Session Refresh and Continuity

Session refresh preserves authentication continuity without re-authentication.

- Refresh operations MUST validate the underlying session.
- Refresh MUST NOT silently elevate privileges.
- Refresh behavior MUST respect the active authentication mode.

Refresh mechanisms MUST NOT bypass session validation or authority evaluation.

---

### 6.6 Relationship Between Sessions and Authentication Modes

Authentication modes define how session state is represented and transported, not how it is validated.

- All authentication modes (except PureJwt) rely on session-backed validation.
- Token-based representations MUST be verifiable against session state.
- Switching authentication modes MUST NOT change session semantics.

Session architecture remains consistent across all modes.

---

### 6.7 Security Invariants

The following invariants MUST hold:

- Sessions are the single source of authenticated identity.
- Session validation occurs server-side.
- Revocation is immediate and deterministic.
- Tokens never replace session authority.
- No authentication flow may bypass session validation.

Any implementation that violates these invariants is considered architecturally incorrect.

## 7. Client & Runtime Model

UltimateAuth defines a single, unified authentication model that operates consistently across multiple client runtimes.
Client runtimes influence *how* authentication flows are executed, but MUST NOT redefine authentication semantics.

---

### 7.1 Runtime-Agnostic Core

The UltimateAuth core authentication model is runtime-agnostic.

- Authentication semantics are defined on the server.
- Client runtimes do not own identity or authentication state.
- Runtime differences MUST NOT result in divergent authentication models.

All clients participate in the same authentication and session architecture regardless of platform.

---

### 7.2 Supported Client Runtimes

UltimateAuth supports multiple client runtimes, including
but not limited to:

- Blazor Server
- Blazor WebAssembly
- MAUI
- MVC applications
- API and headless clients

Support for multiple runtimes is achieved through adaptation, not duplication of authentication logic.

---

### 7.3 Client Profiles

Runtime-specific behavior is expressed through Client Profiles.

- Client Profiles define runtime-appropriate defaults.
- Client Profiles are automatically detected when possible.
- Client Profiles MAY be explicitly configured when required.

Client Profiles influence transport mechanisms, flow selection, and security constraints, but MUST NOT change core authentication semantics.

---

### 7.4 Request-Based Client Participation

Clients participate in authentication on a per-request basis.

- Each request is evaluated independently.
- Client-provided data is treated as input, not authority.
- Authentication state is resolved server-side for every request.

No client runtime is permitted to cache or infer authentication authority outside server validation.

---

### 7.5 Public Clients and PKCE Requirements

Public clients (including browser-based and mobile clients) are treated as untrusted environments.

- Public clients MUST NOT hold secrets.
- PKCE is REQUIRED for authorization flows involving public clients.
- Authentication flows MUST assume client compromise is possible.

Security guarantees MUST be preserved even in the presence of malicious or compromised clients.

---

### 7.6 Client SDK Responsibilities

Client SDKs and libraries provide convenience and integration support only.

- Client SDKs MUST NOT create identity.
- Client SDKs MUST NOT evaluate authorization decisions.
- Client SDKs MUST NOT bypass authentication or session validation.

Client SDKs are adapters, not security authorities.

---

### 7.7 Cross-Runtime Consistency Guarantees

The following guarantees MUST hold across all runtimes:

- Authentication semantics are identical across clients.
- Session validation remains server-authoritative.
- Revocation behavior is consistent across runtimes.
- Runtime-specific optimizations MUST NOT weaken security.

Any implementation that introduces runtime-specific authentication semantics is considered architecturally incorrect.

## 8. Security Invariants

The following security invariants define the non-negotiable security guarantees of UltimateAuth.
These invariants apply across all authentication modes, client runtimes, domains and implementations.
Violating any invariant compromises system security and is considered architecturally incorrect.

---

### 8.1 Server Authority Invariant

The server is the sole authority for authentication and authorization decisions.

- Clients are never trusted authorities.
- Client-provided data is always treated as untrusted input.
- Authentication state MUST be validated server-side.

No client runtime, SDK, or application code may assume authority over identity or access decisions.

---

### 8.2 Session Authority Invariant

Sessions are the single authoritative source of authenticated identity.

- Authentication state MUST be session-backed.
- Tokens, cookies, or headers are transport artifacts only.
- Session validation MUST occur on every authenticated request.

No authentication flow may bypass session validation.

---

### 8.3 Context Integrity Invariant

Authentication and authorization context objects define security boundaries.

- AuthFlowContext is immutable after creation.
- Exactly one AuthFlowContext exists per request.
- AccessContext is derived from AuthFlowContext.
- Context objects MUST NOT be mutated, recreated or bypassed.

Context integrity is mandatory for deterministic and secure request processing.

---

### 8.4 Orchestration Invariant

All security-relevant operations MUST be orchestrated.

- No security-sensitive action may execute directly against stores or domain models.
- All such actions MUST pass through an orchestrator and an authority component.
- Orchestration enforces sequencing, policy evaluation and cross-domain consistency.

Bypassing orchestration or authority evaluation is explicitly forbidden.

---

### 8.5 Domain Isolation Invariant

Domains represent isolated security and responsibility boundaries.

- Domains MUST NOT share mutable state.
- Domains MUST NOT directly modify other domains.
- Cross-domain operations MUST be coordinated through services and orchestrators.

Domain isolation MUST NOT be weakened by persistence or implementation convenience.

---

### 8.6 Credential Protection Invariant

Credential material is security-critical and requires strict isolation.

- Secrets MUST NOT be exposed outside the Credentials domain.
- Credentials MUST NOT be treated as identifiers or profiles.
- Credential validation MUST occur server-side.

Credential leakage or reuse across domains is forbidden.

---

### 8.7 Deterministic Evaluation Invariant

Authentication and authorization evaluation MUST be deterministic.

- Identical inputs MUST produce identical outcomes.
- Hidden, implicit, or ambient security state is not allowed.
- Concurrent or retried requests MUST behave consistently.

Deterministic evaluation is required for correctness, auditability and security.

---

### 8.8 Revocation Invariant

Revocation is a first-class security operation.

- Revoked sessions or credentials MUST be rejected immediately.
- Revocation MUST be enforceable across all runtimes and authentication modes.
- Best-effort or eventual revocation is not permitted.

Revocation guarantees MUST NOT be weakened for performance or convenience.

---

### 8.9 Extensibility Safety Invariant

UltimateAuth is extensible, but extensibility MUST NOT alter security semantics.

- Extensions MUST preserve all security invariants.
- Overrides MUST NOT bypass orchestration, authority or session validation.
- Custom implementations MUST remain server-authoritative.

Extensibility that compromises security guarantees is not supported.

## 9. What This Document Does NOT Define

This document intentionally limits its scope to architectural rules and security invariants.
The following concerns are explicitly out of scope and MUST NOT be inferred from this document.

---

### 9.1 User Experience and Application Flow

This document does NOT define:

- User interface design or layout.
- User experience flows.
- Screen navigation or interaction patterns.
- Application-specific onboarding or registration flows.

Such concerns are application responsibilities and vary by use case.

---

### 9.2 API Shapes and Public Contracts

This document does NOT define:

- Public API method signatures.
- DTO shapes or transport contracts.
- Client SDK APIs or surface area.
- HTTP endpoint structures or routing conventions.

API design MAY evolve as long as architectural and security rules are preserved.

---

### 9.3 Persistence and Infrastructure

This document does NOT define:

- Database technologies or providers.
- Schema designs or table layouts.
- Caching strategies.
- Replication, sharding, or scaling approaches.
- Hosting or deployment topology.

Persistence and infrastructure choices MUST NOT alter architectural or security guarantees.

---

### 9.4 Performance Optimizations

This document does NOT define:

- Performance tuning strategies.
- Caching heuristics or TTL policies.
- Latency optimizations.
- Resource allocation strategies.

Performance improvements MUST preserve all architectural and security invariants.

---

### 9.5 Feature Set and Product Roadmap

This document does NOT define:

- Feature completeness.
- Supported scenarios.
- Roadmap priorities or timelines.
- Backward compatibility guarantees.

Product direction is defined separately and MUST NOT override architectural constraints.

---

### 9.6 Implementation Techniques

This document does NOT define:

- Framework-specific implementation patterns.
- Language-level constructs or idioms.
- Code organization or folder structure.
- Testing strategies or tooling.

Implementation techniques are free to evolve within the boundaries defined by this document.

---

### 9.7 Documentation and Educational Content

This document does NOT define:

- Tutorials or onboarding materials.
- Example applications.
- Reference guides or walkthroughs.

Educational content MUST explain the architecture but MUST NOT redefine it.

---

### 9.8 Final Authority Statement

If a concern is not explicitly defined in this document, it is considered an implementation or product decision, not an architectural rule.

This document exists to constrain behavior, not to describe every possible behavior.


---

## Change Policy

This document is expected to change rarely.
Any change to this document MUST be intentional, explicit, and reviewed with extreme care.
Incremental refactors, convenience changes, or stylistic improvements MUST NOT modify the architectural rules defined here.
